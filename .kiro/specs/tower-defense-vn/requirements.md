# Requirements Document

> Tài liệu Yêu cầu - Tower Defense Việt Nam

## Introduction

Tower Defense Việt Nam (Cổ Sử Việt Hùng - viết tắt CSVH) là game thủ thành 2D góc nhìn isometric/2.5D, lấy cảm hứng từ văn hóa và lịch sử Việt Nam. Người chơi điều khiển một Thành đặt cố định ở góc Đông Nam của màn chơi, chống lại các đợt Quái tiến công từ phía Tây và Tây Bắc. Trò chơi có hệ thống đợt sóng vô tận (Đợt 1 đến vô cực), cấp độ Thành, Máu, Kinh nghiệm (EXP), điểm số phiên hiện tại và kỷ lục cao nhất, cùng ba nhánh nâng cấp: Giáp (phòng thủ), Công (tấn công), Special (chiêu đặc biệt).

Tài liệu này định nghĩa các yêu cầu chức năng và phi chức năng của hệ thống theo chuẩn EARS, tuân thủ quy tắc chất lượng INCOSE, đồng thời nêu các thuộc tính đúng đắn (correctness properties) phục vụ Property-Based Testing.

## Glossary

- **Game**: Toàn bộ phần mềm Tower Defense Việt Nam đang vận hành.
- **Thành**: Công trình phòng thủ duy nhất do người chơi sở hữu, đặt cố định tại góc Đông Nam của Sân_Đấu. Thành có thuộc tính Cấp_Thành, Máu_Hiện_Tại, Máu_Tối_Đa, Giáp, Công, Special, EXP_Hiện_Tại, EXP_Cần_Cấp.
- **Sân_Đấu**: Vùng chơi 2D có hệ tọa độ với gốc ở giữa; trục X dương về phía Đông, trục Y dương về phía Bắc. Bốn góc tương ứng: Tây Bắc (-X, +Y), Đông Bắc (+X, +Y), Tây Nam (-X, -Y), Đông Nam (+X, -Y).
- **Vị_Trí_Thành**: Tọa độ cố định của Thành nằm trong góc Đông Nam của Sân_Đấu (X > 0 và Y < 0).
- **Cổng_Spawn**: Tập hợp các điểm spawn nằm trên biên Bắc, biên Tây, hoặc góc Tây Bắc của Sân_Đấu. Một Cổng_Spawn hợp lệ có (X ≤ 0) hoặc (Y ≥ 0).
- **Quái**: Đơn vị tấn công do Game tạo ra tại một Cổng_Spawn. Quái có thuộc tính Loại_Quái, Máu_Quái, Tốc_Độ, Sát_Thương_Cận_Chiến, Phần_Thưởng_Vàng, Phần_Thưởng_EXP, Phần_Thưởng_Điểm.
- **Đường_Đi_Quái**: Quỹ đạo di chuyển của một Quái từ Cổng_Spawn tiến về Vị_Trí_Thành.
- **Đợt**: Một nhóm Quái được spawn theo cấu hình (số lượng, loại, nhịp spawn) trong một khoảng thời gian. Đợt được đánh số từ 1 và tăng vô hạn (∞).
- **Pha_Chuẩn_Bị**: Khoảng thời gian giữa hai Đợt, có Đếm_Ngược hiển thị cho người chơi.
- **Đếm_Ngược**: Bộ đếm thời gian giảm dần (giây) hiển thị thời gian còn lại trước khi Đợt kế tiếp bắt đầu.
- **Đạn**: Vật thể được Thành phóng ra để gây sát thương lên Quái. Đạn có thuộc tính Vị_Trí, Vận_Tốc, Sát_Thương_Cơ_Bản, Loại_Đạn, Mục_Tiêu (tùy chọn).
- **Va_Chạm**: Sự kiện khi vùng bao của một Đạn giao với vùng bao của một Quái.
- **Sát_Thương_Hiệu_Quả**: Sát thương thực tế Quái nhận sau khi áp dụng các hệ số (Công của Thành, kháng của Quái, hệ số Special).
- **Giáp**: Thuộc tính giảm Sát_Thương_Hiệu_Quả mà Thành nhận từ đòn đánh của Quái.
- **Công**: Thuộc tính tăng Sát_Thương_Hiệu_Quả mà Đạn của Thành gây ra cho Quái.
- **Special**: Chiêu đặc biệt do Thành kích hoạt, có Thời_Gian_Hồi và hiệu ứng diện rộng.
- **EXP**: Điểm kinh nghiệm của Thành. Khi EXP_Hiện_Tại đạt EXP_Cần_Cấp, Thành tăng Cấp_Thành và EXP_Hiện_Tại được điều chỉnh theo công thức trong Yêu cầu 4.
- **Cấp_Thành**: Số nguyên ≥ 1 biểu thị cấp độ hiện tại của Thành.
- **Điểm_Phiên**: Điểm số của lượt chơi hiện tại, được cộng dồn khi tiêu diệt Quái và hoàn thành Đợt.
- **Kỷ_Lục**: Giá trị Điểm_Phiên cao nhất từng đạt được, lưu trữ qua các phiên chơi.
- **Vàng**: Tài nguyên trong trận do Quái rớt ra, dùng để mua nâng cấp Giáp/Công/Special.
- **Bộ_Lưu_Trữ**: Hệ thống đọc/ghi dữ liệu bền vững (Kỷ_Lục, cấu hình âm lượng) trên thiết bị người chơi.
- **HUD**: Lớp giao diện người dùng hiển thị thông tin trận đấu (đợt, đếm ngược, máu, EXP, điểm, nâng cấp).
- **Bộ_Nạp_Cấu_Hình**: Thành phần đọc và phân tích các tệp cấu hình JSON định nghĩa Đợt và Loại_Quái.
- **Bộ_Xuất_Cấu_Hình**: Thành phần ghi (pretty print) cấu hình Đợt/Loại_Quái thành JSON đúng cú pháp.
- **Bộ_Văn_Hóa**: Tập hợp dữ liệu chủ đề Việt Nam (tên Quái, tên skill Special, tên loại Đạn, art, âm thanh) được tham chiếu bởi Game.

## Requirements

### Requirement 1: Bố cục Sân Đấu và Camera Isometric

**User Story:** Là một người chơi, tôi muốn nhìn thấy Thành nằm ở góc Đông Nam và Quái tiến vào từ phía Tây/Tây Bắc theo góc nhìn chéo, để có cảm giác chiến trường rõ ràng và phù hợp với phong cách 2.5D.

#### Acceptance Criteria

1. THE Game SHALL đặt Vị_Trí_Thành tại tọa độ có X > 0 và Y < 0 trong hệ tọa độ Sân_Đấu.
2. THE Game SHALL hiển thị Sân_Đấu bằng camera với góc nhìn isometric/2.5D (hình chiếu trục đo, các trục X-Y bị nghiêng so với trục màn hình).
3. WHEN một Quái được tạo, THE Game SHALL đặt Quái tại một Cổng_Spawn thỏa mãn điều kiện (X ≤ 0) hoặc (Y ≥ 0).
4. IF một Cổng_Spawn được cấu hình với (X > 0 và Y < 0), THEN THE Bộ_Nạp_Cấu_Hình SHALL từ chối cấu hình và trả về thông báo lỗi mô tả vi phạm.
5. THE Game SHALL render Thành ở lớp sắp xếp (sorting order) cao hơn nền cỏ và thấp hơn các hiệu ứng Đạn cùng vị trí.

### Requirement 2: Hệ thống Quái và Đường Đi

**User Story:** Là một người chơi, tôi muốn các Quái di chuyển từ phía Bắc/Tây Bắc/Tây tiến về Thành, để trận đấu có hướng tấn công nhất quán.

#### Acceptance Criteria

1. WHEN một Quái được tạo, THE Game SHALL gán cho Quái một Đường_Đi_Quái có điểm đầu là Cổng_Spawn và điểm cuối là Vị_Trí_Thành.
2. WHILE Quái còn Máu_Quái > 0 và chưa tới Vị_Trí_Thành, THE Game SHALL di chuyển Quái dọc Đường_Đi_Quái với Tốc_Độ của Quái.
3. WHEN Quái chạm Vị_Trí_Thành (khoảng cách Euclid ≤ bán kính va chạm Thành), THE Game SHALL trừ Sát_Thương_Cận_Chiến của Quái vào Máu_Hiện_Tại của Thành sau khi áp dụng Giáp, sau đó loại bỏ Quái khỏi Sân_Đấu.
4. WHEN Máu_Quái của một Quái giảm xuống ≤ 0, THE Game SHALL loại bỏ Quái khỏi Sân_Đấu và cộng Phần_Thưởng_Vàng, Phần_Thưởng_EXP, Phần_Thưởng_Điểm vào Thành và Điểm_Phiên.
5. THE Game SHALL hỗ trợ ít nhất 5 Loại_Quái khác nhau với Tốc_Độ, Máu_Quái, Sát_Thương_Cận_Chiến khác biệt.
6. IF cấu hình Quái có Tốc_Độ ≤ 0 hoặc Máu_Quái ≤ 0, THEN THE Bộ_Nạp_Cấu_Hình SHALL từ chối cấu hình và trả về thông báo lỗi mô tả trường vi phạm.

### Requirement 3: Hệ thống Đạn và Va Chạm

**User Story:** Là một người chơi, tôi muốn Thành tự động bắn Đạn về phía Quái và gây sát thương khi trúng, để tiêu diệt Quái mà không cần thao tác thủ công liên tục.

#### Acceptance Criteria

1. WHILE có ít nhất một Quái còn sống trong Sân_Đấu, THE Thành SHALL phóng một Đạn theo nhịp Tốc_Độ_Bắn (đạn/giây) hiện tại của Thành.
2. WHEN một Đạn được phóng, THE Thành SHALL gán cho Đạn một Vận_Tốc có hướng từ Vị_Trí_Thành đến vị trí của Mục_Tiêu tại thời điểm phóng.
3. WHEN một Đạn xảy ra Va_Chạm với một Quái, THE Game SHALL tính Sát_Thương_Hiệu_Quả = max(0, Sát_Thương_Cơ_Bản × hệ_số_Công − kháng_của_Quái) và trừ giá trị này vào Máu_Quái.
4. WHEN một Đạn xảy ra Va_Chạm với một Quái hoặc rời khỏi biên Sân_Đấu, THE Game SHALL loại bỏ Đạn khỏi Sân_Đấu trong cùng frame.
5. IF Sát_Thương_Cơ_Bản của một Đạn được cấu hình < 0, THEN THE Bộ_Nạp_Cấu_Hình SHALL từ chối cấu hình và trả về thông báo lỗi.
6. THE Game SHALL đảm bảo mỗi Đạn chỉ gây sát thương cho mỗi Quái tối đa một lần trên toàn bộ vòng đời của Đạn đó.

### Requirement 4: Cấp Thành và EXP

**User Story:** Là một người chơi, tôi muốn Thành lên cấp khi tích lũy đủ EXP, để cảm nhận sự tiến bộ qua mỗi trận và mở khóa sức mạnh mới.

#### Acceptance Criteria

1. THE Thành SHALL khởi tạo với Cấp_Thành = 1, EXP_Hiện_Tại = 0, EXP_Cần_Cấp = EXP_Cần_Cấp_Cơ_Bản (ví dụ 100).
2. WHEN Thành nhận EXP từ một Quái bị tiêu diệt, THE Game SHALL cộng Phần_Thưởng_EXP vào EXP_Hiện_Tại.
3. WHILE EXP_Hiện_Tại ≥ EXP_Cần_Cấp, THE Game SHALL trừ EXP_Cần_Cấp khỏi EXP_Hiện_Tại, tăng Cấp_Thành thêm 1, và cập nhật EXP_Cần_Cấp = ceil(EXP_Cần_Cấp × hệ_số_thang_cấp), với hệ_số_thang_cấp ≥ 1.0.
4. THE Game SHALL hiển thị Cấp_Thành dưới dạng văn bản theo định dạng "Cấp: {N}" với N là số nguyên không âm.
5. THE Game SHALL hiển thị tỉ lệ EXP_Hiện_Tại / EXP_Cần_Cấp dưới dạng vòng tròn tiến trình ở góc Trái Dưới của HUD, với giá trị hiển thị trong khoảng [0, 1].
6. IF EXP_Cần_Cấp được cấu hình ≤ 0, THEN THE Bộ_Nạp_Cấu_Hình SHALL từ chối cấu hình và trả về thông báo lỗi.

### Requirement 5: Máu Thành và Sát Thương Nhận

**User Story:** Là một người chơi, tôi muốn theo dõi Máu của Thành theo định dạng "X/Y", để biết khi nào trận đấu sắp kết thúc.

#### Acceptance Criteria

1. THE Thành SHALL khởi tạo với Máu_Hiện_Tại = Máu_Tối_Đa, với Máu_Tối_Đa > 0.
2. WHEN một Quái gây Sát_Thương_Cận_Chiến lên Thành, THE Game SHALL tính Sát_Thương_Nhận = max(0, Sát_Thương_Cận_Chiến − Giáp) và trừ giá trị này vào Máu_Hiện_Tại.
3. THE Game SHALL giới hạn Máu_Hiện_Tại trong khoảng [0, Máu_Tối_Đa] sau mọi cập nhật.
4. WHEN Máu_Hiện_Tại đạt 0, THE Game SHALL chuyển sang trạng thái Kết_Thúc_Trận và dừng spawn Quái mới.
5. THE Game SHALL hiển thị Máu theo định dạng "{Máu_Hiện_Tại}/{Máu_Tối_Đa}" tại HUD vị trí Giữa Dưới.
6. WHERE người chơi mua nâng cấp Giáp làm tăng Máu_Tối_Đa, THE Game SHALL tăng đồng thời Máu_Hiện_Tại theo lượng tăng của Máu_Tối_Đa, không vượt quá Máu_Tối_Đa mới.

### Requirement 6: Nâng cấp - Giáp, Công, Special

**User Story:** Là một người chơi, tôi muốn dùng Vàng để nâng cấp Giáp, Công và Special, để tùy chỉnh chiến lược phòng thủ.

#### Acceptance Criteria

1. THE Game SHALL cung cấp đúng ba nhánh nâng cấp: Giáp, Công, Special.
2. WHEN người chơi chọn một nhánh nâng cấp và Vàng ≥ Giá_Nâng_Cấp_Hiện_Tại của nhánh đó, THE Game SHALL trừ Giá_Nâng_Cấp_Hiện_Tại khỏi Vàng và tăng Cấp_Nâng_Cấp của nhánh thêm 1.
3. IF người chơi chọn nâng cấp và Vàng < Giá_Nâng_Cấp_Hiện_Tại, THEN THE Game SHALL giữ nguyên Vàng và Cấp_Nâng_Cấp, đồng thời hiển thị thông báo "Không đủ Vàng".
4. WHEN Cấp_Nâng_Cấp Giáp tăng, THE Game SHALL cộng dồn lượng giảm sát thương theo công thức Giáp = Giáp_Cơ_Bản + Cấp_Nâng_Cấp_Giáp × Bước_Tăng_Giáp.
5. WHEN Cấp_Nâng_Cấp Công tăng, THE Game SHALL cập nhật hệ_số_Công = 1 + Cấp_Nâng_Cấp_Công × Bước_Tăng_Công.
6. WHEN người chơi kích hoạt Special và Thời_Gian_Hồi_Còn_Lại = 0, THE Game SHALL áp dụng hiệu ứng Special lên tất cả Quái trong Bán_Kính_Special và đặt Thời_Gian_Hồi_Còn_Lại = Thời_Gian_Hồi_Tối_Đa.
7. WHILE Thời_Gian_Hồi_Còn_Lại > 0, THE Game SHALL từ chối kích hoạt Special và hiển thị Thời_Gian_Hồi_Còn_Lại trên icon Special.
8. THE Game SHALL hiển thị bốn icon trong khu vực Giữa Dưới của HUD theo thứ tự cố định: Công, Giáp, Special, EXP.

### Requirement 7: Hệ thống Đợt Sóng (Wave)

**User Story:** Là một người chơi, tôi muốn các đợt Quái xuất hiện theo nhịp với khoảng nghỉ rõ ràng, để có thời gian nâng cấp giữa các đợt.

#### Acceptance Criteria

1. THE Game SHALL bắt đầu trận đấu ở Đợt số 1.
2. WHEN tất cả Quái của Đợt hiện tại đã bị loại bỏ khỏi Sân_Đấu, THE Game SHALL bắt đầu Pha_Chuẩn_Bị với Đếm_Ngược = Thời_Gian_Chuẩn_Bị_Đợt (giây).
3. WHILE Pha_Chuẩn_Bị đang diễn ra, THE Game SHALL hiển thị "Đợt kế tiếp: {N+1}" và "Đếm ngược: {giây}" tại khu vực Trên Cùng của HUD.
4. WHEN Đếm_Ngược đạt 0, THE Game SHALL tăng số Đợt thêm 1 và bắt đầu spawn Quái của Đợt mới.
5. THE Game SHALL không có Đợt cuối cùng; số Đợt tăng vô hạn miễn là Game chưa ở trạng thái Kết_Thúc_Trận.
6. THE Game SHALL hiển thị "Đợt {N}/∞" tại khu vực Trên Cùng của HUD trong suốt Đợt đang diễn ra.
7. WHERE Đợt là bội số của 5, THE Game SHALL spawn thêm một Quái trùm (boss) có Máu_Quái và Sát_Thương_Cận_Chiến cao hơn Quái thường ít nhất 5 lần.

### Requirement 8: Điểm số và Kỷ Lục

**User Story:** Là một người chơi, tôi muốn xem điểm phiên hiện tại và kỷ lục cao nhất, để có động lực phá kỷ lục.

#### Acceptance Criteria

1. THE Game SHALL khởi tạo Điểm_Phiên = 0 ở thời điểm bắt đầu trận đấu.
2. WHEN một Quái bị tiêu diệt, THE Game SHALL cộng Phần_Thưởng_Điểm của Quái vào Điểm_Phiên.
3. WHEN một Đợt được hoàn thành (tất cả Quái của Đợt bị loại bỏ), THE Game SHALL cộng Thưởng_Hoàn_Thành_Đợt × số_Đợt vào Điểm_Phiên.
4. THE Game SHALL hiển thị "Điểm: {Điểm_Phiên}" và "Cao nhất: {Kỷ_Lục}" tại khu vực Trên Phải của HUD.
5. WHEN trận đấu kết thúc và Điểm_Phiên > Kỷ_Lục, THE Game SHALL cập nhật Kỷ_Lục = Điểm_Phiên và lưu vào Bộ_Lưu_Trữ.
6. WHEN Game khởi động, THE Game SHALL đọc Kỷ_Lục từ Bộ_Lưu_Trữ; IF không có giá trị nào tồn tại, THEN THE Game SHALL khởi tạo Kỷ_Lục = 0.

### Requirement 9: Bố cục HUD

**User Story:** Là một người chơi, tôi muốn HUD bố trí thông tin nhất quán theo các vùng cố định, để dễ quan sát mà không bị che khuất chiến trường.

#### Acceptance Criteria

1. THE HUD SHALL hiển thị "Đợt {N}/∞" và Đếm_Ngược tại vùng Trên Cùng.
2. THE HUD SHALL hiển thị Điểm_Phiên và Kỷ_Lục tại vùng Trên Phải.
3. THE HUD SHALL hiển thị avatar Quái hoặc avatar cấp Quái hiện tại tại vùng Trên Trái.
4. THE HUD SHALL hiển thị "Cấp: {Cấp_Thành}" và vòng tiến trình EXP tại vùng Dưới Trái.
5. THE HUD SHALL hiển thị Máu theo định dạng "{Máu_Hiện_Tại}/{Máu_Tối_Đa}" và bốn icon nâng cấp Công, Giáp, Special, EXP tại vùng Giữa Dưới.
6. THE HUD SHALL hiển thị Thành và đại bác tại vùng Dưới Phải khớp với Vị_Trí_Thành.
7. WHILE độ phân giải màn hình thay đổi, THE HUD SHALL giữ nguyên vị trí tương đối các vùng (Trên Trái, Trên Cùng, Trên Phải, Dưới Trái, Giữa Dưới, Dưới Phải) thông qua hệ neo (anchors).

### Requirement 10: Bộ nạp / Bộ xuất cấu hình JSON (Đợt và Quái)

**User Story:** Là một nhà thiết kế, tôi muốn định nghĩa Đợt và Loại_Quái bằng tệp JSON và có thể đọc/ghi tệp một cách an toàn, để dễ cân bằng game mà không sửa mã nguồn.

#### Acceptance Criteria

1. WHEN Game khởi động, THE Bộ_Nạp_Cấu_Hình SHALL đọc tệp `waves.json` và `enemies.json` từ thư mục cấu hình của ứng dụng.
2. WHEN Bộ_Nạp_Cấu_Hình nhận một tệp JSON hợp lệ theo lược đồ, THE Bộ_Nạp_Cấu_Hình SHALL trả về một đối tượng cấu hình tương ứng.
3. IF Bộ_Nạp_Cấu_Hình nhận một tệp JSON sai cú pháp hoặc sai lược đồ, THEN THE Bộ_Nạp_Cấu_Hình SHALL trả về một mã lỗi mô tả dòng/cột và lý do.
4. THE Bộ_Xuất_Cấu_Hình SHALL chuyển một đối tượng cấu hình hợp lệ thành chuỗi JSON tuân thủ lược đồ.
5. FOR ALL đối tượng cấu hình hợp lệ C, THE hệ thống SHALL bảo đảm Bộ_Nạp_Cấu_Hình(Bộ_Xuất_Cấu_Hình(C)) trả về đối tượng tương đương C theo quan hệ bằng_giá_trị (round-trip property).
6. FOR ALL chuỗi JSON hợp lệ S đã được Bộ_Xuất_Cấu_Hình tạo ra, THE hệ thống SHALL bảo đảm Bộ_Xuất_Cấu_Hình(Bộ_Nạp_Cấu_Hình(S)) tạo ra chuỗi tương đương S sau khi chuẩn hóa khoảng trắng.

### Requirement 11: Văn hóa Việt Nam

**User Story:** Là một người chơi, tôi muốn cảm nhận yếu tố văn hóa Việt Nam qua tên, hình ảnh và âm thanh, để game có bản sắc riêng.

#### Acceptance Criteria

1. THE Bộ_Văn_Hóa SHALL cung cấp tối thiểu 5 tên Loại_Quái lấy cảm hứng từ truyền thuyết hoặc lịch sử Việt Nam (ví dụ Thuồng_Luồng, Quân_Tống, Quân_Nguyên_Mông, Hồ_Tinh, Mộc_Tinh).
2. THE Bộ_Văn_Hóa SHALL cung cấp tối thiểu 3 tên skill Special lấy cảm hứng từ văn hóa Việt Nam (ví dụ "Trống_Đồng_Đông_Sơn", "Lưỡi_Gươm_Lê_Lợi", "Mũi_Tên_An_Dương_Vương").
3. WHERE người chơi chọn ngôn ngữ tiếng Việt, THE Game SHALL hiển thị toàn bộ chuỗi UI bằng tiếng Việt có dấu (UTF-8).
4. THE Game SHALL sử dụng bảng màu và hoa văn lấy cảm hứng từ trống đồng Đông Sơn cho khung HUD và các icon nâng cấp.
5. THE Game SHALL phát nhạc nền sử dụng nhạc cụ truyền thống Việt Nam (ví dụ đàn bầu, sáo trúc, trống) trong các Đợt thường.

### Requirement 12: Lưu trữ và Cấu hình Phiên

**User Story:** Là một người chơi, tôi muốn các thiết lập âm lượng và Kỷ_Lục được giữ lại giữa các phiên chơi, để không phải cấu hình lại mỗi lần.

#### Acceptance Criteria

1. WHEN người chơi thay đổi âm lượng nhạc hoặc hiệu ứng, THE Game SHALL ghi giá trị mới vào Bộ_Lưu_Trữ trong vòng 1 giây.
2. WHEN Game khởi động, THE Game SHALL đọc các giá trị âm lượng từ Bộ_Lưu_Trữ; IF chưa có giá trị, THEN THE Game SHALL gán giá trị mặc định 1.0.
3. THE Game SHALL giới hạn các giá trị âm lượng đọc/ghi trong khoảng [0.0, 1.0].
4. IF Bộ_Lưu_Trữ trả về dữ liệu hỏng (parse JSON thất bại), THEN THE Game SHALL khôi phục giá trị mặc định và ghi log cảnh báo.

### Requirement 13: Cân bằng và Hiệu năng

**User Story:** Là một người chơi, tôi muốn game chạy mượt và phản hồi nhanh trong các Đợt đông Quái, để không cảm thấy giật lag.

#### Acceptance Criteria

1. WHILE số Quái trong Sân_Đấu ≤ 200, THE Game SHALL duy trì tốc độ khung hình ≥ 60 FPS trên thiết bị tham chiếu (cấu hình tham chiếu được ghi trong tài liệu kỹ thuật).
2. WHEN người chơi nhấn vào icon nâng cấp, THE Game SHALL phản hồi (cập nhật trạng thái và HUD) trong vòng 100 ms.
3. THE Game SHALL giới hạn số Đạn đồng thời tồn tại ≤ 500.
4. IF số Quái yêu cầu spawn vượt quá 200, THEN THE Game SHALL hoãn các Quái dôi vào hàng đợi và spawn khi số Quái hiện tại < 200.

## Correctness Properties cho Property-Based Testing

Phần này liệt kê các thuộc tính đúng đắn nên được kiểm thử bằng Property-Based Testing (ví dụ FsCheck cho C#). Mỗi thuộc tính nêu rõ đầu vào sinh ngẫu nhiên và bất biến cần giữ.

### P1. Round-trip cấu hình JSON (Requirement 10)
- Đầu vào: cấu hình `WaveConfig` hoặc `EnemyConfig` hợp lệ được sinh ngẫu nhiên.
- Tính chất: `Load(Save(c))` trả về đối tượng bằng giá trị với `c` cho mọi `c` hợp lệ.
- Tính chất kép: với mọi chuỗi JSON `s` được sinh từ `Save`, `Save(Load(s))` chuẩn hóa khoảng trắng cho ra `s` ban đầu.

### P2. Bất biến giới hạn Máu (Requirement 5)
- Đầu vào: chuỗi sự kiện ngẫu nhiên gồm sát thương dương và hồi máu dương trên Thành có Máu_Tối_Đa cố định.
- Tính chất: sau mọi cập nhật, `0 ≤ Máu_Hiện_Tại ≤ Máu_Tối_Đa`.

### P3. Bất biến vị trí spawn của Quái (Requirement 1, 2)
- Đầu vào: tập Cổng_Spawn hợp lệ được sinh ngẫu nhiên.
- Tính chất: với mọi Quái được tạo, vị trí spawn thỏa `(X ≤ 0) ∨ (Y ≥ 0)` và Vị_Trí_Thành thỏa `(X > 0) ∧ (Y < 0)`.

### P4. Đơn điệu của Cấp_Thành theo EXP (Requirement 4)
- Đầu vào: chuỗi giá trị EXP cộng dồn không âm.
- Tính chất: hàm `LevelAfter(expSequence)` đơn điệu không giảm theo độ dài tiền tố của chuỗi (thêm EXP không thể làm giảm Cấp_Thành).
- Bất biến phụ: sau mọi bước, `0 ≤ EXP_Hiện_Tại < EXP_Cần_Cấp`.

### P5. Idempotence của hành động "Hủy Đạn ngoài biên" (Requirement 3)
- Đầu vào: tập Đạn ngẫu nhiên với vị trí và vận tốc bất kỳ.
- Tính chất: `Cull(Cull(world)) == Cull(world)` sau khi hủy các Đạn đã rời biên Sân_Đấu.

### P6. Bảo toàn tổng sát thương trên một Đạn (Requirement 3)
- Đầu vào: một Đạn và một danh sách Quái nó đi qua trong vòng đời.
- Tính chất: tổng sát thương Đạn gây ra ≤ `Sát_Thương_Cơ_Bản × hệ_số_Công` × số_Quái_va_chạm_duy_nhất, và mỗi Quái nhận sát thương từ Đạn này tối đa một lần.

### P7. Confluence của thứ tự áp dụng nâng cấp (Requirement 6)
- Đầu vào: hai hoán vị bất kỳ của cùng một tập nâng cấp Giáp/Công có thể chi trả.
- Tính chất: trạng thái cuối (Giáp, Công, Vàng) độc lập với thứ tự áp dụng (vì mỗi nâng cấp là phép cộng giao hoán).

### P8. Mô hình hóa wave: số Quái spawn = số Quái cấu hình (Requirement 7)
- Đầu vào: cấu hình Đợt sinh ngẫu nhiên với danh sách `(LoạiQuái, sốLượng)`.
- Tính chất: sau khi Đợt kết thúc và bộ điều phối hoàn tất, tổng số Quái đã spawn = tổng `sốLượng` trong cấu hình; số Quái còn sống = 0 hoặc tất cả đã đến Vị_Trí_Thành.

### P9. Đơn điệu của Điểm_Phiên (Requirement 8)
- Đầu vào: chuỗi sự kiện gồm tiêu diệt Quái và hoàn thành Đợt.
- Tính chất: `Điểm_Phiên` đơn điệu không giảm theo thời gian; chỉ thay đổi khi có sự kiện cộng điểm.

### P10. Round-trip Kỷ_Lục qua Bộ_Lưu_Trữ (Requirement 8, 12)
- Đầu vào: số nguyên không âm `k` ≤ 2^31 − 1.
- Tính chất: `Storage.LoadHighScore(Storage.SaveHighScore(k)) == k`.

### P11. Bất biến Vàng không âm (Requirement 6)
- Đầu vào: chuỗi sự kiện cộng/trừ Vàng tuân theo quy tắc Yêu cầu 6.
- Tính chất: với mọi bước, `Vàng ≥ 0`.

### P12. Bất biến số Đợt tăng nghiêm ngặt (Requirement 7)
- Đầu vào: chuỗi sự kiện kết thúc Đợt và Đếm_Ngược về 0.
- Tính chất: số Đợt là dãy đơn điệu tăng nghiêm ngặt theo thời gian; không tồn tại trạng thái mà số Đợt giảm.

### P13. Bất biến tham chiếu văn hóa (Requirement 11)
- Đầu vào: tập định danh Loại_Quái và skill Special được sinh ngẫu nhiên từ Bộ_Văn_Hóa.
- Tính chất: mọi định danh được tham chiếu đều tồn tại trong Bộ_Văn_Hóa và mọi mục trong Bộ_Văn_Hóa được sử dụng tại ít nhất một vị trí (no-orphan, no-dangling).

---

Lưu ý: Các thuộc tính P1, P5, P7, P10 là round-trip / idempotence / confluence — đặc biệt phù hợp với Property-Based Testing. P2, P4, P9, P11, P12 là các bất biến kiểm tra dễ dàng. P8 là kiểm thử dựa trên mô hình (Model-Based Testing) cho hệ thống đợt sóng.
