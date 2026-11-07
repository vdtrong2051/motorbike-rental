#!/usr/bin/env python3
"""GD3 browser-level smoke: cookie roles, antiforgery, public + admin moderation."""
import http.cookiejar
import json
import os
import sys
from html.parser import HTMLParser
from urllib import error, parse, request

BASE = os.environ.get('GD3_BASE_URL', 'http://127.0.0.1:5120').rstrip('/')

class FormToken(HTMLParser):
    def __init__(self):
        super().__init__()
        self.token = None
    def handle_starttag(self, tag, attributes):
        if tag == 'input':
            d = dict(attributes)
            if d.get('name') == '__RequestVerificationToken':
                self.token = d.get('value')

class Browser:
    def __init__(self):
        self.opener = request.build_opener(request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    def send(self, path, fields=None):
        payload = parse.urlencode(fields).encode() if fields is not None else None
        req = request.Request(BASE + path, data=payload)
        try:
            with self.opener.open(req, timeout=20) as response:
                return response.status, response.url, response.read().decode('utf-8', errors='replace')
        except error.HTTPError as exc:
            return exc.code, exc.url, exc.read().decode('utf-8', errors='replace')
    def form_token(self, path):
        code, url, body = self.send(path)
        assert code == 200, f'GET {path} returned {code} {url}'
        parser = FormToken(); parser.feed(body)
        assert parser.token, f'No antiforgery token: {path}'
        return parser.token
    def login(self, email, password):
        token = self.form_token('/Account/Login')
        status, url, _ = self.send('/Account/Login', {
            '__RequestVerificationToken': token, 'Email': email,
            'Password': password, 'RememberMe': 'false'
        })
        assert status == 200 and parse.urlsplit(url).path == '/', f'Login failed: {status} {url}'

def check(ok, label):
    assert ok, 'GD3 FAIL: ' + label
    print('PASS', label)

def main():
    with open(sys.argv[1], encoding='utf-8') as f:
        data = json.load(f)
    user = Browser(); user.login(data['email'], data['password'])
    other = Browser(); other.login(data['otherEmail'], data['password'])
    owner = Browser(); owner.login(data['ownerEmail'], data['password'])
    admin = Browser(); admin.login(data['adminEmail'], data['password'])
    anonymous = Browser()
    review_url = '/OrderReview/Create?itemId=' + data['itemId']

    code, url, _ = anonymous.send(review_url)
    check(parse.urlsplit(url).path == '/Account/Login', 'Anonymous customer redirected to login')
    check(other.send(review_url)[0] == 404, 'Other customer cannot open review form')
    check(owner.send(review_url)[0] != 200 or
          parse.urlsplit(owner.send(review_url)[1]).path != '/OrderReview/Create',
          'Owner cannot open customer review form')

    check(data['vehicleId'] and user.send(review_url)[0] == 200,
          'Own completed item review form renders')
    token = user.form_token(review_url)
    code, _, page = user.send('/OrderReview/Create', {
        '__RequestVerificationToken': token,
        'RentalOrderItemId': data['itemId'], 'SoSao': '0', 'NhanXet': 'Invalid stars'
    })
    check(code == 200 and '1 đến 5' in page, 'Invalid rating rejected by MVC')
    code, url, page = user.send('/OrderReview/Create', {
        '__RequestVerificationToken': token,
        'RentalOrderItemId': data['itemId'], 'SoSao': '5', 'NhanXet': 'GD3 browser submitted review'
    })
    check(code == 200 and 'RentalOrder/Details' in url and 'GD3 browser submitted review' not in page,
          'Completed order review submits and redirects to order details')
    code, url, _ = user.send(review_url)
    check(code == 200 and 'RentalOrder/Details' in url, 'Second review form request redirected')

    code, _, html = owner.send('/OrderReview/OwnerReviews')
    check(code == 200 and 'GD3 browser submitted review' in html,
          'Owner sees new review for owned vehicle')
    code, _, html = admin.send('/OrderReview/AdminReviews?stars=4')
    check(code == 200 and data['comment'] in html, 'Admin star filter shows seeded review')
    code, _, html = admin.send('/OrderReview/AdminReviews')
    check(code == 200 and data['comment'] in html, 'Admin moderation view renders')
    token_admin = admin.form_token('/OrderReview/AdminReviews')
    code, url, _ = admin.send('/OrderReview/SetVisibility', {
        '__RequestVerificationToken': token_admin, 'reviewId': data['moderationId'],
        'visible': 'false'
    })
    check(code == 200 and parse.urlsplit(url).path == '/OrderReview/AdminReviews',
          'Admin hide action redirects successfully')
    code, _, html = anonymous.send('/Vehicle/Details/' + data['vehicleId'])
    check(code == 200 and data['comment'] not in html,
          'Hidden review excluded from public vehicle page')

    code, url, _ = owner.send('/OrderReview/SetVisibility', {
        'reviewId': data['moderationId'], 'visible': 'true'
    })
    check(parse.urlsplit(url).path != '/OrderReview/AdminReviews',
          'Owner cannot invoke admin moderation')

    token_admin = admin.form_token('/OrderReview/AdminReviews')
    code, url, _ = admin.send('/OrderReview/SetVisibility', {
        '__RequestVerificationToken': token_admin, 'reviewId': data['moderationId'],
        'visible': 'true'
    })
    check(code == 200 and parse.urlsplit(url).path == '/OrderReview/AdminReviews',
          'Admin unhide action accepted')
    code, _, html = anonymous.send('/Vehicle/Details/' + data['vehicleId'])
    check(code == 200 and data['comment'] in html,
          'Restored review publicly visible')
    print('GD3 E2E PASS (Customer, Owner, Admin, moderation, MySQL).')

if __name__ == '__main__':
    try: main()
    except Exception as exc:
        print('GD3 E2E FAIL:', str(exc), file=sys.stderr)
        sys.exit(1)
