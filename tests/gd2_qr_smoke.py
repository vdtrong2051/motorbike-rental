#!/usr/bin/env python3
"""GD2 HTTP smoke across cookies, QR PNG, bearer token, role isolation and state polling."""
import http.cookiejar
import json
import os
import sys
from html.parser import HTMLParser
from urllib import error, parse, request

BASE = os.getenv('GD2_BASE_URL', 'http://127.0.0.1:5119').rstrip('/')

class TokenParser(HTMLParser):
    def __init__(self):
        super().__init__(); self.token = None
    def handle_starttag(self, tag, attrs):
        if tag == 'input':
            d = dict(attrs)
            if d.get('name') == '__RequestVerificationToken':
                self.token = d.get('value')

class Browser:
    def __init__(self):
        self.opener = request.build_opener(request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    def req(self, path, fields=None, js=None):
        headers = {'Accept': 'application/json' if js is not None else 'text/html'}
        if js is not None:
            data = json.dumps(js).encode(); headers['Content-Type'] = 'application/json'
        elif fields is not None:
            data = parse.urlencode(fields).encode(); headers['Content-Type'] = 'application/x-www-form-urlencoded'
        else: data = None
        req = request.Request(BASE + path, data=data, headers=headers)
        try:
            with self.opener.open(req, timeout=30) as res:
                return res.status, res.url, res.read(), dict(res.headers.items())
        except error.HTTPError as exc:
            return exc.code, exc.url, exc.read(), dict(exc.headers.items())
    def token(self, path):
        code, url, body, _ = self.req(path)
        assert code == 200, f'GET {path} => {code} {url}'
        parser = TokenParser(); parser.feed(body.decode('utf8', errors='replace'))
        assert parser.token, f'Antiforgery input missing: {path}'
        return parser.token
    def login(self, email, password):
        token = self.token('/Account/Login')
        code, url, body, _ = self.req('/Account/Login', {
            '__RequestVerificationToken': token, 'Email': email, 'Password': password,
            'RememberMe': 'false'
        })
        assert code == 200 and parse.urlsplit(url).path == '/', f'Login failure {code} {url}: {body[:180]}'

def check(value, label):
    assert value, 'GD2 FAIL: ' + label
    print('PASS', label)

def session(browser, oid):
    code, _, body, _ = browser.req('/api/payments/demo/session?orderId=' + oid)
    check(code == 200, 'Customer can load a signed demo session')
    data = json.loads(body)
    check(data['simulated'] is True and data['amount'] == 240000, 'Session indicates DEMO and server-derived amount')
    return data['token']

def checkout(browser, token):
    path = '/PaymentDemo/Checkout?token=' + parse.quote(token, safe='')
    return browser.req(path)

def complete(browser, token, outcome):
    return browser.req('/api/payments/demo/complete', js={'token': token, 'outcome': outcome})

def state(browser, oid):
    code, _, body, _ = browser.req('/api/payments/orders/' + oid + '/status')
    check(code == 200, 'Customer sees own payment status')
    return json.loads(body)

def main():
    with open(sys.argv[1], encoding='utf8') as file: f = json.load(file)
    customer = Browser(); customer.login(f['email'], f['password'])
    other = Browser(); other.login(f['otherEmail'], f['password'])
    anon = Browser()

    # QR is dynamically generated on the server and does not expose its token to CDNs.
    qrpath = '/PaymentDemo/Qr?orderId=' + f['paid']
    code, _, png, headers = customer.req(qrpath)
    check(code == 200 and png.startswith(b'\x89PNG\r\n\x1a\n') and len(png) > 500,
          'Real scannable-format QR PNG generated server-side')
    check('no-store' in headers.get('Cache-Control', '').lower(), 'QR cannot be cached')
    check(other.req(qrpath)[0] == 404, 'Other customer cannot generate victim QR')
    check(other.req('/api/payments/orders/' + f['paid'] + '/status')[0] == 404,
          'Other customer cannot read victim payment status')
    check(anon.req(qrpath)[0] != 200, 'Anonymous visitor cannot generate a new QR')

    token = session(customer, f['paid'])
    code, url, body, _ = customer.req('/PaymentDemo/Start?orderId=' + f['paid'])
    check(code == 200 and parse.urlsplit(url).path == '/PaymentDemo/Checkout',
          'Same-device entry opens mobile checkout')
    code, _, html, headers = checkout(anon, token)
    check(code == 200 and 'KHÔNG CHUYỂN TIỀN THẬT' in html.decode('utf8') and b'data-gd2-demo' in html,
          'Anonymous phone can open clearly labeled DEMO landing page')
    check(headers.get('Referrer-Policy') == 'no-referrer' and 'no-store' in headers.get('Cache-Control', ''),
          'Landing page blocks token referrer and caching')
    check('default-src' in headers.get('Content-Security-Policy', ''), 'Landing page has CSP')
    code, _, body, _ = complete(anon, token, 'PAID')
    check(code == 200 and json.loads(body)['paid'] is True,
          'Anonymous phone sends token-authenticated success to PaymentCore')
    check(state(customer, f['paid'])['paid'], 'Computer polling observes paid status')
    check(checkout(anon, token)[0] == 410, 'Paid QR landing cannot be reused to start a new payment')
    check(complete(anon, token, 'PAID')[0] == 200, 'Duplicate callback is harmless and idempotent')

    failtoken = session(customer, f['failed'])
    check(complete(anon, failtoken, 'FAILED')[0] == 200, 'Wallet DEMO supports FAILED result')
    check(state(customer, f['failed'])['status'] == 'ThatBai', 'Failed wallet status reaches desktop')
    check(checkout(anon, failtoken)[0] == 410, 'Old failed QR becomes invalid')
    path = '/OrderPayment/Index?orderId=' + f['failed']
    anti = customer.token(path)
    code, _, _, _ = customer.req('/OrderPayment/Select', {
        '__RequestVerificationToken': anti, 'OrderId': f['failed'], 'PhuongThuc': 'TheThanhToan'
    })
    check(code == 200, 'Failed wallet can switch to card demo')
    cardtoken = session(customer, f['failed'])
    check(cardtoken != failtoken, 'New attempt has a different bearer token')
    check(complete(anon, failtoken, 'PAID')[0] == 409, 'Superseded old token cannot confirm new attempt')
    check(complete(anon, cardtoken, 'PAID')[0] == 200, 'Card DEMO can be confirmed')

    cancelToken = session(customer, f['cancelled'])
    anti = customer.token('/RentalOrder/Details/' + f['cancelled'])
    code, _, _, _ = customer.req('/RentalOrder/Cancel', {
        '__RequestVerificationToken': anti, 'id': f['cancelled'], 'reason': 'GD2 test'
    })
    check(code == 200, 'Customer can cancel own pending order')
    check(customer.req('/PaymentDemo/Qr?orderId=' + f['cancelled'])[0] == 404,
          'Cancelled order cannot generate QR')
    check(checkout(anon, cancelToken)[0] == 410, 'Cancelled QR landing is invalid')
    check(complete(anon, cancelToken, 'PAID')[0] == 409, 'Callback after cancel rejected')
    check(state(customer, f['cancelled'])['orderStatus'] == 'DaHuy', 'Desktop detects cancellation')

    check(customer.req('/PaymentDemo/Start?orderId=' + f['expired'])[0] == 404,
          'Expired attempt cannot issue demo link')
    check(customer.req('/PaymentDemo/Qr?orderId=' + f['expired'])[0] == 404,
          'Expired attempt cannot issue QR')
    check(checkout(anon, token + 'tamper')[0] == 410, 'Tampered signed token refused')
    check(complete(anon, token + 'tamper', 'PAID')[0] == 401, 'Forged token refused by API')
    print('GD2 QR PAYMENT E2E PASS (HTTP+MySQL, no new schema).')

if __name__ == '__main__':
    try: main()
    except Exception as exc:
        print('GD2 QR PAYMENT E2E FAIL:', str(exc), file=sys.stderr)
        sys.exit(1)
