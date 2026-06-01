# Lịch sử cập nhật: Cơ chế ngắm bắn & Tối ưu UI/Camera

Dưới đây là danh sách tổng hợp toàn bộ các tính năng và sửa lỗi đã được thực hiện trong phiên làm việc này.

## 1. Cơ chế Ngắm Bắn (Tap & Drag to Aim)
- **Tập tin:** `TowerView.cs`
- **Thay đổi:** 
  - **Đổi cơ chế ngắm:** Xóa bỏ việc sử dụng 2 phím cứng (Trái/Phải). Đổi sang cơ chế chạm và vuốt (Tap & Drag) trực tiếp trên màn hình.
  - **Vuốt liên tục (Drag):** Nòng pháo sẽ quay và bám theo ngón tay (chuột) một cách liên tục và mượt mà khi người chơi giữ và di chuyển.
  - **Đường ngắm nét đứt (Dashed Line):** Cập nhật `LineRenderer` của tia ngắm từ dạng nét liền sang dạng nét đứt. Tính năng này được thực hiện bằng cách tạo một Texture2D (trong suốt một nửa) bằng code ngay lúc chạy, kết hợp với chế độ `LineTextureMode.Tile`.

## 2. Ngăn chặn Nhấn Xuyên UI (Block Raycast)
- **Tập tin:** `HUDController.cs` và `TowerView.cs`
- **Thay đổi:**
  - **Vấn đề cũ:** Khi người chơi bấm vào các nút UI (Nâng cấp, Skill) nằm ở góc, nòng pháo vẫn bị bắt điểm chạm và bẻ góc ngắm một cách không mong muốn.
  - **Giải pháp:** Bổ sung hàm `IsPointerOverUI` trong `HUDController`. Hàm này dùng `RuntimePanelUtils` để quy đổi toạ độ chuột và `panel.Pick()` để kiểm tra xem có phần tử UI Toolkit nào đang nằm dưới ngón tay không.
  - Nhờ đó, nếu người chơi tap vào vùng UI, game sẽ bỏ qua hoàn toàn thao tác điều khiển nòng pháo, giữ nguyên hướng ngắm cũ.

## 3. Tối ưu UI Toolkit Scale (Hỗ trợ màn hình 4K)
- **Tập tin:** `PanelSettings.asset`
- **Thay đổi:** 
  - **Vấn đề cũ:** UI không co giãn theo độ phân giải do đang đặt ở chế độ `Constant Physical Size`. Lên màn 4K các nút bấm bị thu lại siêu nhỏ.
  - **Giải pháp:** Đổi `Scale Mode` sang `Scale With Screen Size` và gán `Reference Resolution` thành mốc chuẩn `1920x1080` (Match Height).
  - Kết quả là UI Toolkit giờ đây tự động scale to/nhỏ tỉ lệ thuận với màn hình (hoạt động giống hệt Canvas Scaler).

## 4. Neo Vị Trí Thành (Tower) Theo Camera (Hỗ trợ đa tỉ lệ màn hình)
- **Tập tin:** `GameSceneRoot.cs`
- **Thay đổi:** 
  - Thêm xử lý trong hàm `LateUpdate` để liên tục kiểm tra và tính toán lại tỉ lệ khung hình (Aspect Ratio) của màn hình hiện tại so với chuẩn 16:9 (`1920 / 1080`).
  - Dựa trên hệ số này để tự động dịch chuyển trục X của `Camera.main`.
  - Tác dụng: Dù người chơi dùng màn hình iPad (4:3) hay màn hình Ultrawide (21:9), Thành (Tower) vẫn luôn nằm gọn gàng ở vị trí góc Đông Nam y như lúc thiết kế ở tỉ lệ 16:9, không bị lệch vào giữa hay văng ra khỏi khung hình.

## 5. Dọn dẹp Code (Cleanup)
- **Tập tin:** `HUDController.cs` và `TowerView.cs`
- **Thay đổi:** Xóa bỏ toàn bộ các dòng log gỡ lỗi (`Debug.Log`) tạm thời, giúp trả lại Console sạch sẽ.
