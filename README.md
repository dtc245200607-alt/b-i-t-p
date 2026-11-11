# EduSubmit – Submission API và Client

Hai chương trình độc lập giao tiếp qua HTTP:

- `src/Submission.Api` – Minimal API trên ASP.NET Core .NET 10, cổng 5000.
- `src/Submission.Client` – ứng dụng console dùng `HttpClient`.

Dữ liệu mẫu và lượt nộp được giữ trong RAM; khởi động lại API sẽ xóa các lượt nộp.

## Hợp đồng API

| Phương thức | URI | Thành công | Lỗi nghiệp vụ |
|---|---|---|---|
| GET | `/api/assignments` | 200 danh sách | — |
| GET | `/api/assignments/{assignmentId}` | 200 bài tập | 404 `ASSIGNMENT_NOT_FOUND` |
| POST | `/api/assignments/{assignmentId}/submissions` | 201 + `Location` | 404 `ASSIGNMENT_NOT_FOUND`, 400 `ASSIGNMENT_CLOSED`, 400 `INVALID_SUBMISSION` |
| GET | `/api/submissions/{submissionId}` | 200 lượt nộp | 404 `SUBMISSION_NOT_FOUND` |

Lỗi nghiệp vụ dùng chung dạng `{ "code": "...", "message": "..." }`. Khi nộp bài, API kiểm tra lần lượt: bài tập tồn tại, bài còn mở, rồi dữ liệu có mã sinh viên và URL repository không rỗng. API tự tạo ID, trạng thái `received` và thời điểm nhận; client không được gửi các trường này.

## Build và chạy

Từ thư mục repo:

```bash
dotnet build MiniDistributedSystem.slnx
```

Terminal 1:

```bash
dotnet run --project src/Submission.Api --urls http://localhost:5000
```

Terminal 2:

```bash
dotnet run --project src/Submission.Client -- http://localhost:5000
```

Client thực hiện chuỗi: lấy danh sách → đọc chi tiết bài tập → nộp bài → đọc lại theo `Location` → gửi thử body không hợp lệ. Lỗi HTTP được đọc thành `ApiError`; lỗi kết nối được báo riêng.

`requests.http` có các request cho 200, 201, 400 và 404. Chạy API trước, sau đó dùng REST Client trong VS Code để gửi từng request; request đọc lại lấy ID từ phản hồi của `createSubmission`.

## Giải thích

1. URI đặt tên tài nguyên (bài tập, lượt nộp); phương thức HTTP diễn tả thao tác như đọc (`GET`) hay tạo (`POST`), nên URI không cần chứa động từ.
2. Mỗi yêu cầu mang đủ thông tin cần xử lý qua URI và body. Server không dựa vào trạng thái hội thoại ở client; danh sách lượt nộp là trạng thái tài nguyên phía server và hiện chỉ lưu trong RAM.
3. Client và server khai báo DTO riêng để giữ hai tiến trình độc lập. Chúng khớp nhờ cùng tên trường và kiểu JSON theo hợp đồng (ví dụ `StudentCode` ↔ `studentCode`), không cần chia sẻ mã nguồn.
