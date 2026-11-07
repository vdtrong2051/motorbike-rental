#!/usr/bin/env python3
"""Dependency-free structural checks; MySQL integration is verified separately."""
from pathlib import Path
import base64, hashlib, hmac, json, re
root=Path(__file__).resolve().parents[1]
sql=(root/'demo/seed-demo.sql').read_text(encoding='utf-8')
fixture=json.loads((root/'demo/demo-qa-fixture.json').read_text(encoding='utf-8'))
expected_tables=['Users','Vehicles','VehicleImages','RentalOrders','RentalOrderItems','RentalOrderContracts','RentalOrderPayments','RentalOrderItemReviews']
assert all(sql.count(f'INSERT INTO `{table}`')==1 for table in expected_tables)
assert 'USE `RentalPlatformDemo`' in sql
assert 'USE `RentalPlatformDb`' not in sql
expected_status=['ChoDuyet','SanSang','DangThue','BaoTri','TuChoi']
assert all(sql.count(f"'{status}'")>=2 for status in expected_status)
for status in ['ChoChuXeXacNhan','DaXacNhan','ChoThanhToan','DaThanhToan','DangThue','HoanThanh','DaHuy']:
    assert f"'{status}'" in sql, status
for email in ['admin@rentalplatform.vn','owner1@rental.demo','owner2@rental.demo','customer1@rental.demo','customer2@rental.demo','customer3@rental.demo']:
    assert sql.count("'"+email+"'")>=1, email
    assert email in fixture.values()
# Verify all Identity V3 PBKDF2 hashes actually match the documented passwords.
for hashed, password in [
 ('AQAAAAIAAYagAAAAEJmbj3JDPyFwWJt95tNlkptcKNFD8z8Ej/00VPVI0IGYxmkhdtmbDlBwj7n+GPPRbQ==','Admin@123'),
 ('AQAAAAIAAYagAAAAEDUSxGB8J177/tMw/A3xkRBctuEx1fhPSK01vD+JttvVagcq8tlDzKO35bwGASjAwg==','Owner@123'),
 ('AQAAAAIAAYagAAAAEOAlEtVfgE9ms/9LxJY6sK9t6yEo9o4aME+9vjR3tqxXNUxsDStq1sVZSmdTBd2rOQ==','Customer@123')
]:
    raw=base64.b64decode(hashed)
    assert raw[0]==1 and int.from_bytes(raw[1:5],'big')==2
    rounds=int.from_bytes(raw[5:9],'big'); length=int.from_bytes(raw[9:13],'big')
    salt=raw[13:13+length]; subkey=raw[13+length:]
    actual=hashlib.pbkdf2_hmac('sha512', password.encode('utf-8'),salt,rounds,len(subkey))
    assert hmac.compare_digest(actual,subkey)
    assert hashed in sql
assert all(c<128 for c in (root/'demo/setup-demo.ps1').read_bytes())
assert (root/'demo/verify-demo.sql').read_text().count('SIGNAL SQLSTATE')>=10
print('DEMO STATIC PASS: six accounts, 5 vehicle statuses, 7 order statuses, 3 verified password hashes.')
