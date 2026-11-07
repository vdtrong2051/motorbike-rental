#!/usr/bin/env python3
"""GD4.1–4.6: role UI wiring, responsive and single-Bootstrap contract check."""
from pathlib import Path
root = Path(__file__).resolve().parents[2]

def require(rel,*parts):
    p=root/rel
    if not p.is_file(): raise AssertionError(f'Missing GD4 resource: {rel}')
    s=p.read_text(encoding='utf-8')
    for part in parts:
        assert part in s, f'{rel} missing {part!r}'
    return s

market = require('RentalPlatform/Views/Shared/_Gd4MarketplaceLayout.cshtml','bootstrap@5.3.3','gd4-marketplace.css','_Gd4MarketplaceNav','RenderBody()','RenderSectionAsync')
workspace = require('RentalPlatform/Views/Shared/_Gd4WorkspaceLayout.cshtml','@@tabler/core@@1.7.3','gd4-workspace.css','_Gd4WorkspaceSidebar','RenderBody()','RenderSectionAsync')
assert market.count('bootstrap.bundle.min.js') == 1 and 'tabler.min' not in market
assert workspace.count('tabler.min.js') == 1
assert 'bootstrap.bundle' not in workspace.replace('<!-- @tabler/core already includes Bootstrap: do not add bootstrap.min.css or bundle.js here. -->','')
require('RentalPlatform/Views/_ViewStart.cshtml','_Gd4MarketplaceLayout','_Gd4WorkspaceLayout','User.IsInRole("Owner")','User.IsInRole("Admin")')
require('RentalPlatform/Views/Shared/_Gd4WorkspaceSidebar.cshtml','OwnerOrders','AdminReviews','PendingVehicles')
require('RentalPlatform/Views/Home/Index.cshtml','gd4-hero','name="NgayNhanXe"','name="NgayTraXe"','name="KhuVuc"')
require('RentalPlatform/Views/Vehicle/Index.cshtml','gd4-search-layout','data-gd4-filter','data-gd4-date-range')
require('RentalPlatform/Views/Vehicle/Details.cshtml','gd4-vehicle-detail','Cart')
require('RentalPlatform/Views/Cart/Index.cshtml','gd4-cart-page','cùng một chủ')
require('RentalPlatform/Views/RentalOrder/Checkout.cshtml','gd4-checkout-page','grandTotal')
require('RentalPlatform/Views/RentalOrder/Details.cshtml','_Gd4OrderJourney','gd4-order-page','OrderReview')
require('RentalPlatform/Views/OrderPayment/Index.cshtml','gd4-payment-page','gd2-payment')
require('RentalPlatform/Views/OrderReview/Create.cshtml','gd4-review-page')
require('RentalPlatform/Views/OwnerPortal/Dashboard.cshtml','gd4-action-strip','OwnerOrders')
require('RentalPlatform/Views/Admin/Dashboard.cshtml','gd4-action-strip','PendingVehicles')
require('RentalPlatform/wwwroot/css/gd4-responsive.css','max-width: 767.98px','gd4-journey-steps')
require('RentalPlatform/wwwroot/css/gd4-core.css','prefers-reduced-motion','focus-visible')
require('RentalPlatform/wwwroot/js/gd4-interactions.js','setCustomValidity','data-gd4-filter')
require('scripts/qa/gd4-live-audit.mjs','playwright','viewport','scrollWidth','main-content')
assert (root/'db/gd3_order_item_reviews.sql').exists(), 'GĐ3 SQL must remain available; no automatic migration'
print('GD4 UI STATIC CONTRACT PASS — role layouts, journeys, mobile, accessibility, single Bootstrap bundle.')
