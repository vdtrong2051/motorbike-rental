# Triển khai RentalPlatform bằng Nginx trên Ubuntu

Kiến trúc:

`Tên miền -> Nginx :80/:443 -> Kestrel 127.0.0.1:5000 -> MySQL 127.0.0.1:3306`

Chỉ mở cổng 22, 80 và 443. Không mở cổng 5000 hoặc 3306 ra Internet khi
ứng dụng và MySQL chạy trên cùng VPS.

## 1. Trỏ tên miền

Tại nhà cung cấp tên miền, tạo hai bản ghi:

- `A`, host `@`, value là IPv4 của VPS.
- `A`, host `www`, value là IPv4 của VPS.

## 2. Kết nối và cài phần mềm

Từ PowerShell trên Windows:

```powershell
ssh root@VPS_IP
```

Trên Ubuntu VPS:

```bash
sudo apt update
sudo apt upgrade -y
sudo apt install -y nginx mysql-server certbot python3-certbot-nginx
sudo apt install -y aspnetcore-runtime-10.0
dotnet --list-runtimes
```

Kết quả phải có `Microsoft.AspNetCore.App 10.x`. Nếu Ubuntu không tìm thấy gói
runtime, thêm kho gói Microsoft theo tài liệu dành cho đúng phiên bản Ubuntu.

## 3. Tạo MySQL

```bash
sudo mysql
```

Chạy các câu sau trong MySQL và thay mật khẩu mạnh:

```sql
CREATE DATABASE RentalPlatformDb
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
CREATE USER 'rentalapp'@'localhost' IDENTIFIED BY 'CHANGE_THIS_PASSWORD';
GRANT ALL PRIVILEGES ON RentalPlatformDb.* TO 'rentalapp'@'localhost';
FLUSH PRIVILEGES;
EXIT;
```

## 4. Publish và tải file từ Windows lên VPS

Chạy tại thư mục `RentalPlatform-GitHub\RentalPlatform`:

```powershell
dotnet publish -c Release -o .\publish
ssh root@VPS_IP "mkdir -p /tmp/rentalplatform /tmp/rentalplatform-deploy"
scp -r .\publish\* root@VPS_IP:/tmp/rentalplatform/
scp .\database-init-mysql.sql root@VPS_IP:/tmp/rentalplatform-deploy/
scp .\deploy\rentalplatform.env.example root@VPS_IP:/tmp/rentalplatform-deploy/
scp .\deploy\systemd\rentalplatform.service root@VPS_IP:/tmp/rentalplatform-deploy/
scp .\deploy\nginx\rentalplatform.conf root@VPS_IP:/tmp/rentalplatform-deploy/
```

## 5. Khởi tạo bảng và cài ứng dụng trên VPS

```bash
mysql -u rentalapp -p RentalPlatformDb < /tmp/rentalplatform-deploy/database-init-mysql.sql
sudo useradd --system --home /var/lib/rentalplatform --create-home --shell /usr/sbin/nologin rentalplatform
sudo mkdir -p /var/www/rentalplatform/current /etc/rentalplatform /var/lib/rentalplatform/identity-documents
sudo cp -a /tmp/rentalplatform/. /var/www/rentalplatform/current/
sudo chown -R rentalplatform:rentalplatform /var/www/rentalplatform /var/lib/rentalplatform
```

Nếu `useradd` báo tài khoản đã tồn tại thì bỏ qua lỗi đó và chạy tiếp.

## 6. Cấu hình mật khẩu và systemd

```bash
sudo cp /tmp/rentalplatform-deploy/rentalplatform.env.example /etc/rentalplatform/rentalplatform.env
sudo nano /etc/rentalplatform/rentalplatform.env
```

Thay ba giá trị `CHANGE_THIS...` và email quản trị. Mật khẩu MySQL trong connection
string phải giống mật khẩu đã tạo ở bước 3. Sau đó:

```bash
sudo chown root:rentalplatform /etc/rentalplatform/rentalplatform.env
sudo chmod 640 /etc/rentalplatform/rentalplatform.env
sudo cp /tmp/rentalplatform-deploy/rentalplatform.service /etc/systemd/system/rentalplatform.service
sudo systemctl daemon-reload
sudo systemctl enable --now rentalplatform
sudo systemctl status rentalplatform --no-pager
```

Nếu service lỗi:

```bash
sudo journalctl -u rentalplatform -n 100 --no-pager
```

## 7. Cấu hình Nginx

```bash
sudo cp /tmp/rentalplatform-deploy/rentalplatform.conf /etc/nginx/sites-available/rentalplatform
sudo nano /etc/nginx/sites-available/rentalplatform
```

Thay toàn bộ `rentalplatform.example.com` bằng tên miền thật rồi chạy:

```bash
sudo ln -s /etc/nginx/sites-available/rentalplatform /etc/nginx/sites-enabled/rentalplatform
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx
```

Truy cập `http://TEN_MIEN` để kiểm tra trước khi bật HTTPS.

## 8. Bật HTTPS miễn phí

```bash
sudo certbot --nginx -d TEN_MIEN -d www.TEN_MIEN
sudo certbot renew --dry-run
```

Nếu không sử dụng `www`, xóa `www.TEN_MIEN` khỏi Nginx và lệnh Certbot.

## 9. Bật firewall

```bash
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
sudo ufw status
```

Không mở `5000/tcp` hoặc `3306/tcp`.

## 10. Cập nhật source lần sau

Publish lại và tải nội dung thư mục `publish` vào `/tmp/rentalplatform`, sau đó:

```bash
sudo systemctl stop rentalplatform
sudo cp -a /tmp/rentalplatform/. /var/www/rentalplatform/current/
sudo chown -R rentalplatform:rentalplatform /var/www/rentalplatform
sudo systemctl start rentalplatform
sudo systemctl status rentalplatform --no-pager
```

Không chép `appsettings.json` chứa mật khẩu Production lên GitHub. Production sẽ
ưu tiên connection string từ `/etc/rentalplatform/rentalplatform.env`.
