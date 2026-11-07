#!/usr/bin/env python3
"""GD0 HTTP integration smoke test against an ephemeral CI MySQL instance."""
import http.cookiejar
import os
import sys
import uuid
from html.parser import HTMLParser
from urllib import error, parse, request

BASE = os.environ.get('GD0_BASE_URL', 'http://127.0.0.1:5117').rstrip('/')

class TokenParser(HTMLParser):
    def __init__(self):
        super().__init__()
        self.token = None
    def handle_starttag(self, tag, attrs):
        if tag == 'input':
            data = dict(attrs)
            if data.get('name') == '__RequestVerificationToken':
                self.token = data.get('value')

class Browser:
    def __init__(self):
        self.opener = request.build_opener(request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    def request(self, path, fields=None):
        data = parse.urlencode(fields).encode() if fields is not None else None
        req = request.Request(BASE + path, data=data)
        if data is not None:
            req.add_header('Content-Type', 'application/x-www-form-urlencoded')
        try:
            with self.opener.open(req, timeout=20) as res:
                return res.status, parse.urlparse(res.url).path, res.read().decode('utf-8', errors='replace')
        except error.HTTPError as exc:
            return exc.code, parse.urlparse(exc.url).path, exc.read().decode('utf-8', errors='replace')
    def token(self, path):
        code, landing, body = self.request(path)
        assert code == 200, f'GET {path} got {code} {landing}'
        parser = TokenParser(); parser.feed(body)
        assert parser.token, f'No antiforgery token in {path}'
        return parser.token

def page(browser, path, code=200, landing=None):
    result, dest, _ = browser.request(path)
    assert result == code, f'{path} returned {result} not {code} (landed {dest})'
    if landing is not None:
        assert dest == landing, f'{path} redirected to {dest} instead of {landing}'
    print(f'PASS GET {path} => {result} {dest}')

def register(email, owner):
    browser = Browser()
    token = browser.token('/Account/Register')
    code, dest, body = browser.request('/Account/Register', {
        '__RequestVerificationToken': token,
        'HoTen': 'GD0 Owner' if owner else 'GD0 Customer',
        'Email': email, 'SDT': '0901234567',
        'Password': 'TestUser_2026!', 'ConfirmPassword': 'TestUser_2026!',
        'IsOwner': 'true' if owner else 'false'})
    assert code == 200 and dest == '/Account/Login', f'Register failed: {code} {dest} {body[:200]}'
    print('PASS register', 'Owner' if owner else 'Customer')
    return browser

def login(browser, email, password):
    token = browser.token('/Account/Login')
    code, dest, body = browser.request('/Account/Login', {
        '__RequestVerificationToken': token, 'Email': email, 'Password': password,
        'RememberMe': 'false'})
    assert code == 200 and dest == '/', f'Login {email} failed: {code} {dest} {body[:200]}'
    print('PASS login', email)

def main():
    public = Browser()
    for path in ('/', '/Vehicle/Index', '/Account/Register', '/Account/Login'):
        page(public, path)
    page(public, '/RentalOrder/MyOrders', landing='/Account/Login')

    suffix = uuid.uuid4().hex[:10]
    cemail = f'gd0-customer-{suffix}@example.test'
    oemail = f'gd0-owner-{suffix}@example.test'
    customer = register(cemail, owner=False)
    owner = register(oemail, owner=True)
    login(customer, cemail, 'TestUser_2026!')
    login(owner, oemail, 'TestUser_2026!')

    page(customer, '/RentalOrder/MyOrders')
    page(customer, '/Booking/MyBookings')
    # ReviewController must resolve IReviewService and return 404 for unknown booking.
    page(customer, '/Review/Create?bookingId=00000000-0000-0000-0000-000000000000', code=404)
    page(customer, '/Admin/Dashboard', landing='/Account/AccessDenied')
    page(customer, '/OwnerPortal/Dashboard', landing='/Account/AccessDenied')
    page(owner, '/OwnerPortal/Dashboard')
    page(owner, '/RentalOrder/OwnerOrders')
    page(owner, '/Admin/Dashboard', landing='/Account/AccessDenied')
    page(owner, '/RentalOrder/MyOrders', landing='/Account/AccessDenied')

    admin = Browser()
    login(admin, os.environ.get('SeedAdmin__Email', 'gd0-admin@example.test'),
          os.environ.get('SeedAdmin__Password', 'GD0_Admin_Test_2026!'))
    for path in ('/Admin/Dashboard', '/Admin/Payments', '/Admin/PendingVehicles'):
        page(admin, path)
    page(admin, '/OwnerPortal/Dashboard', landing='/Account/AccessDenied')
    print('GD0 BASELINE SMOKE PASS: Startup, public pages, three roles, review DI.')

if __name__ == '__main__':
    try: main()
    except Exception as exc:
        print('GD0 BASELINE SMOKE FAIL:', str(exc), file=sys.stderr)
        sys.exit(1)
