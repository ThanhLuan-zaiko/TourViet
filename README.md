# 📦 SQL Server Backup & Restore Guide (Docker + WSL)

Tài liệu này hướng dẫn **chi tiết từng bước (step-by-step)** cách **backup (sao lưu)** và **restore (phục hồi)** cơ sở dữ liệu **SQL Server** đang chạy trong **Docker (WSL)** bằng **Azure Data Studio**.

> ✅ Áp dụng cho môi trường:
> - SQL Server chạy trong Docker
> - Quản lý bằng Azure Data Studio
> - Hệ điều hành Windows + WSL (Ubuntu)

---

## 📌 Mục tiêu
- Sao lưu database ra file `.bak`
- Reset database khi cần
- Phục hồi dữ liệu từ file backup
- Đảm bảo an toàn dữ liệu khi test / migrate

---

## 🧰 Yêu cầu môi trường

- Docker Desktop (đã bật WSL2)
- SQL Server container đang chạy
- Azure Data Studio
- Database ví dụ: `TourBookingSystem`

---

## 🗂️ Quy ước đường dẫn backup

Trong container SQL Server, file backup sẽ được lưu tại: /var/opt/mssql/backup/


File backup ví dụ: TourBookingSystem.bak


---

## 🔹 STEP 1: Backup database

Mở **Azure Data Studio** → kết nối SQL Server → mở **New Query**  
Chạy lệnh sau:

```sql
BACKUP DATABASE TourBookingSystem
TO DISK = '/var/opt/mssql/backup/TourBookingSystem.bak'
WITH INIT;

✔ Kết quả

SQL Server tạo file: /var/opt/mssql/backup/TourBookingSystem.bak
File nằm bên trong container Docker

🔹 STEP 2: Chuyển sang database master

⚠️ BẮT BUỘC: Không được restore khi đang sử dụng chính database đó.

```sql
USE master;
GO
```

✔ Kết quả

Con trỏ lệnh chuyển sang database master

🔹 STEP 3: Đưa database về chế độ SINGLE_USER

Mục đích:

Ngắt toàn bộ kết nối

Tránh lỗi database đang được sử dụng

```sql
ALTER DATABASE TourBookingSystem
SET SINGLE_USER
WITH ROLLBACK IMMEDIATE;
GO
```

✔ Kết quả

Database chuyển sang chế độ chỉ 1 người dùng

🔹 STEP 4: Restore database từ file .bak

```sql
RESTORE DATABASE TourBookingSystem
FROM DISK = '/var/opt/mssql/backup/TourBookingSystem.bak'
WITH REPLACE;
GO
```

📌 Giải thích

WITH REPLACE: ghi đè database hiện tại

Dữ liệu sẽ quay về thời điểm backup

🔹 STEP 5: Đặt lại chế độ MULTI_USER

```sql
ALTER DATABASE TourBookingSystem
SET MULTI_USER;
GO
```

✔ Kết quả

Database cho phép nhiều người dùng kết nối

🔹 STEP 6: Kiểm tra kết quả

```sql
USE TourBookingSystem;
GO

SELECT COUNT(*) FROM dbo.Customers;
```

✔ Kết quả

Số lượng bản ghi đúng như lúc backup

---

## 📂 Cách lấy file backup ra Windows

### Phương án 1: Copy từ container ra host

```bash
# 1. Tìm tên container SQL Server
docker ps

# 2. Copy file backup ra thư mục Downloads trên Windows
docker cp <container_name>:/var/opt/mssql/backup/TourBookingSystem.bak D:/Downloads/
```

### Phương án 2: Mount volume từ đầu

Khi tạo container, mount volume:

```bash
docker run -d \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=YourStrongPassword123!" \
  -p 1433:1433 \
  -v D:/sql_backup:/var/opt/mssql/backup \
  --name sqlserver_wsl \
  mcr.microsoft.com/mssql/server:2022-latest
```

Sau đó backup trực tiếp vào thư mục D:/sql_backup

---

## 🔄 Quy trình reset database hoàn chỉnh

```sql
-- 1. Chuyển sang master
USE master;
GO

-- 2. Tắt kết nối đến database cần reset
ALTER DATABASE TourBookingSystem
SET SINGLE_USER
WITH ROLLBACK IMMEDIATE;
GO

-- 3. Xóa database
DROP DATABASE TourBookingSystem;
GO

-- 4. Tạo lại database
CREATE DATABASE TourBookingSystem;
GO

-- 5. Đặt lại chế độ đa người dùng
ALTER DATABASE TourBookingSystem
SET MULTI_USER;
GO

-- 6. Restore từ file backup
RESTORE DATABASE TourBookingSystem
FROM DISK = '/var/opt/mssql/backup/TourBookingSystem.bak'
WITH REPLACE;
GO
```

---

## 🛠️ Troubleshooting

### Lỗi: Database đang được sử dụng

```sql
-- Chạy lệnh này trước khi DROP
ALTER DATABASE TourBookingSystem
SET SINGLE_USER
WITH ROLLBACK IMMEDIATE;
```

### Lỗi: Không tìm thấy file backup

Kiểm tra lại đường dẫn:

```sql
SELECT * FROM sys.dm_os_file_exists('/var/opt/mssql/backup/TourBookingSystem.bak');
```

### Lỗi: Restore thất bại

Kiểm tra log lỗi:

```sql
SELECT * FROM sys.dm_exec_requests
WHERE command LIKE '%RESTORE%'
ORDER BY start_time DESC;
```

---

## 🎯 Tóm tắt lệnh

### Backup

```sql
BACKUP DATABASE TourBookingSystem
TO DISK = '/var/opt/mssql/backup/TourBookingSystem.bak'
WITH INIT;
```

### Restore

```sql
USE master;
GO

RESTORE DATABASE TourBookingSystem
FROM DISK = '/var/opt/mssql/backup/TourBookingSystem.bak'
WITH REPLACE;
GO
```

---

## 📚 Tài liệu tham khảo

- [BACKUP DATABASE (Transact-SQL)](https://learn.microsoft.com/en-us/sql/t-sql/statements/backup-database-transact-sql)
- [RESTORE DATABASE (Transact-SQL)](https://learn.microsoft.com/en-us/sql/t-sql/statements/restore-database-transact-sql)
- [SQL Server on Docker](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-docker-container-get-started)

---

**Document version:** 1.0  
**Last updated:** Thứ 6, ngày 6 tháng 2 năm 2026  
**Author:** Nguyễn Đăng Thành Luân