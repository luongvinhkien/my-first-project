const PDFDocument = require('pdfkit');
const fs = require('fs');
const path = require('path');

// Output file path
const outputPath = path.join(__dirname, '..', 'huong_dan_su_dung.pdf');

// Create document
const doc = new PDFDocument({
    size: 'A4',
    margins: { top: 50, bottom: 50, left: 50, right: 50 }
});

// Stream to file
const writeStream = fs.createWriteStream(outputPath);
doc.pipe(writeStream);

// Register Vietnamese Fonts from Windows Fonts folder
const fontPath = 'C:\\Windows\\Fonts\\arial.ttf';
const fontBoldPath = 'C:\\Windows\\Fonts\\arialbd.ttf';
const fontItalicPath = 'C:\\Windows\\Fonts\\ariali.ttf';

if (fs.existsSync(fontPath) && fs.existsSync(fontBoldPath)) {
    doc.registerFont('Arial', fontPath);
    doc.registerFont('Arial-Bold', fontBoldPath);
    doc.registerFont('Arial-Italic', fontItalicPath);
} else {
    // Fallback if fonts don't exist
    doc.registerFont('Arial', 'Helvetica');
    doc.registerFont('Arial-Bold', 'Helvetica-Bold');
    doc.registerFont('Arial-Italic', 'Helvetica-Oblique');
}

// Helper to draw a horizontal rule
function drawHR(y) {
    doc.strokeColor('#e5e7eb')
       .lineWidth(1)
       .moveTo(50, y)
       .lineTo(545, y)
       .stroke();
}

// ==========================================
// 1. COVER PAGE
// ==========================================
doc.rect(0, 0, 595, 842).fill('#0f172a'); // Slate 900 background

// Accent color top bar
doc.rect(0, 0, 595, 20).fill('#10b981'); // Emerald 500

// Title
doc.font('Arial-Bold').fillColor('#10b981').fontSize(36).text('ẨM THỰC VIỆT', 50, 220, { align: 'center' });
doc.font('Arial-Bold').fillColor('#ffffff').fontSize(24).text('HƯỚNG DẪN SỬ DỤNG PHẦN MỀM', 50, 275, { align: 'center' });

// Decorative Subtitle
doc.font('Arial-Italic').fillColor('#94a3b8').fontSize(14).text('Hệ thống quản lý nhà hàng & POS chuyên nghiệp', 50, 320, { align: 'center' });

// Separation Line
doc.strokeColor('#334155').lineWidth(2).moveTo(150, 370).lineTo(445, 370).stroke();

// Details box
doc.font('Arial').fillColor('#cbd5e1').fontSize(12);
doc.text('Phiên bản: 1.0 (MVC Standard)', 50, 420, { align: 'center' });
doc.text('Cơ sở dữ liệu: SQL Server (Active Sync)', 50, 440, { align: 'center' });
doc.text('Ngôn ngữ hỗ trợ: Tiếng Việt / English / 中文', 50, 460, { align: 'center' });
doc.text('Ngày phát hành: Tháng 07, 2026', 50, 480, { align: 'center' });

// Footer
doc.rect(0, 780, 595, 62).fill('#020617');
doc.font('Arial-Bold').fillColor('#64748b').fontSize(10).text('© 2026 Hệ Thống Quản Lý Ẩm Thực Việt. All rights reserved.', 50, 805, { align: 'center' });

// ==========================================
// 2. PAGE 2: TABLE OF CONTENTS & OVERVIEW
// ==========================================
doc.addPage();
doc.fillColor('#020617'); // reset to normal color

// Page Header
doc.font('Arial-Bold').fillColor('#0f172a').fontSize(18).text('GIỚI THIỆU & MỤC LỤC', 50, 50);
drawHR(75);

doc.font('Arial').fillColor('#334155').fontSize(11).text(
    'Phần mềm Quản lý Nhà hàng Ẩm Thực Việt là giải pháp quản lý toàn diện theo mô hình MVC, kết nối trực tiếp với cơ sở dữ liệu SQL Server. Phần mềm hỗ trợ vận hành đa thiết bị, cập nhật dữ liệu thời gian thực và tích hợp trí tuệ nhân tạo (AI) giúp tối ưu hóa doanh thu và quản lý kho hàng.',
    50, 95, { width: 495, align: 'justify', lineGap: 4 }
);

// Table of Contents Box
doc.rect(50, 180, 495, 260).fill('#f8fafc').stroke('#e2e8f0');

doc.font('Arial-Bold').fillColor('#0f172a').fontSize(13).text('MỤC LỤC CHI TIẾT', 70, 200);
doc.font('Arial').fillColor('#334155').fontSize(11);

const tocItems = [
    { num: 'Chương 1', title: 'Hệ thống Dashboard (Tổng quan kinh doanh)', page: 'Page 3' },
    { num: 'Chương 2', title: 'Quản lý Sơ đồ bàn & Đặt bàn trước (Reservations)', page: 'Page 4' },
    { num: 'Chương 3', title: 'Giao diện Phục vụ Gọi món (Order POS Screen)', page: 'Page 5' },
    { num: 'Chương 4', title: 'Màn hình Bếp & Bar (Kitchen Display System - KDS)', page: 'Page 6' },
    { num: 'Chương 5', title: 'Thanh toán, In hóa đơn & Cổng MBBank VietQR', page: 'Page 7' },
    { num: 'Chương 6', title: 'Quản lý Kho, Công thức định lượng (BOM) & AI', page: 'Page 8' }
];

let tocY = 230;
tocItems.forEach(item => {
    doc.font('Arial-Bold').text(item.num, 70, tocY);
    doc.font('Arial').text(item.title, 140, tocY);
    doc.font('Arial-Italic').fillColor('#64748b').text(item.page, 470, tocY);
    doc.fillColor('#334155');
    tocY += 30;
});

// Guide info footer
doc.font('Arial').fillColor('#64748b').fontSize(10).text('* Để in tài liệu này ra giấy hoặc lưu PDF bản cứng, bạn có thể chọn Print (Ctrl+P) trên trình duyệt.', 50, 750);

// ==========================================
// 3. PAGE 3: DASHBOARD
// ==========================================
doc.addPage();
doc.font('Arial-Bold').fillColor('#0f172a').fontSize(16).text('CHƯƠNG 1: HỆ THỐNG DASHBOARD (TỔNG QUAN)', 50, 50);
drawHR(70);

doc.font('Arial').fillColor('#334155').fontSize(11).text(
    'Màn hình Tổng quan cung cấp thông tin chi tiết về tình hình hoạt động kinh doanh của nhà hàng theo thời gian thực (Real-time).',
    50, 90, { width: 495 }
);

// Bullet points
const dbPoints = [
    { title: 'Chỉ số Kinh doanh chính:', desc: 'Xem doanh thu hôm nay, tổng số lượt khách, số bàn đang phục vụ, bàn trống, và các đơn hàng đang chế biến.' },
    { title: 'Biểu đồ Doanh thu & Lợi nhuận:', desc: 'Biểu đồ trực quan hóa doanh thu theo giờ và lợi nhuận tích lũy 7 ngày qua thông qua Chart.js.' },
    { title: 'Top 5 món bán chạy nhất:', desc: 'Danh sách xếp hạng các món ăn được gọi nhiều nhất trong ngày để có kế hoạch chuẩn bị nguyên liệu tốt hơn.' },
    { title: 'Cảnh báo nguyên liệu sắp hết:', desc: 'Hệ thống tự động quét kho và đưa ra các cảnh báo màu đỏ đối với nguyên liệu chạm ngưỡng an toàn tối thiểu.' }
];

let pointY = 140;
dbPoints.forEach(p => {
    doc.font('Arial-Bold').fillColor('#10b981').text('• ' + p.title, 50, pointY);
    doc.font('Arial').fillColor('#334155').text(p.desc, 70, pointY + 15, { width: 475 });
    pointY += 55;
});

// ==========================================
// 4. PAGE 4: TABLE MAP & RESERVATIONS
// ==========================================
doc.addPage();
doc.font('Arial-Bold').fillColor('#0f172a').fontSize(16).text('CHƯƠNG 2: SƠ ĐỒ BÀN & ĐẶT BÀN TRƯỚC', 50, 50);
drawHR(70);

doc.font('Arial').fillColor('#334155').fontSize(11).text(
    'Phân hệ Sơ đồ bàn hiển thị cấu trúc bàn ăn theo khu vực (Trong nhà, Ngoài trời, Phòng VIP, Lầu 1) và hỗ trợ quản lý trạng thái trực quan.',
    50, 90, { width: 495 }
);

// Table Status Table
doc.font('Arial-Bold').fontSize(12).text('Bảng Trạng Thái Bàn:', 50, 140);

const statusTable = [
    { status: 'Trống (Green)', desc: 'Bàn sẵn sàng đón khách. Bấm vào bàn và chọn "Mở bàn phục vụ khách" để bắt đầu order.' },
    { status: 'Đang phục vụ (Red)', desc: 'Bàn đang có khách ăn uống. Hiển thị tổng số món và tiền tạm tính ngay trên ô bàn.' },
    { status: 'Đã đặt trước (Purple)', desc: 'Bàn được giữ chỗ cho khách đặt trước. Hiển thị thông tin tên và giờ đến của khách.' },
    { status: 'Đang dọn (Yellow)', desc: 'Bàn vừa thanh toán xong, đang được nhân viên dọn dẹp để chuẩn bị đón khách mới.' },
    { status: 'Khóa bàn (Gray)', desc: 'Bàn tạm ngưng phục vụ để bảo trì hoặc ghép bàn.' }
];

let tableY = 170;
statusTable.forEach(s => {
    doc.font('Arial-Bold').fillColor('#0f172a').text(s.status, 60, tableY, { width: 140 });
    doc.font('Arial').fillColor('#334155').text(s.desc, 200, tableY, { width: 330 });
    tableY += 45;
});

// Operations
doc.font('Arial-Bold').fillColor('#0f172a').fontSize(12).text('Thao tác nhanh:', 50, 420);
doc.font('Arial').fillColor('#334155').fontSize(11).text(
    '• Chuyển bàn: Di chuyển toàn bộ giỏ hàng từ bàn cũ sang bàn mới trống.\n' +
    '• Gộp bàn: Gộp hóa đơn của nhiều bàn ăn lại thành một để thanh toán chung.\n' +
    '• Tách bàn: Tách các món ăn trong hóa đơn ra thành một bàn khác biệt.',
    50, 445, { lineGap: 6 }
);

// ==========================================
// 5. PAGE 5: POS ORDER
// ==========================================
doc.addPage();
doc.font('Arial-Bold').fillColor('#0f172a').fontSize(16).text('CHƯƠNG 3: GIAO DIỆN GỌI MÓN (ORDER POS)', 50, 50);
drawHR(70);

doc.font('Arial').fillColor('#334155').fontSize(11).text(
    'Màn hình POS là trung tâm phục vụ chính của nhân viên thu ngân và nhân viên order, hỗ trợ chọn món và gửi yêu cầu nấu trực tiếp xuống bếp.',
    50, 90, { width: 495 }
);

const posSteps = [
    { step: '1. Chọn món ăn:', desc: 'Menu hiển thị ảnh thực tế sinh động kèm bộ lọc theo Danh mục (Món ăn, Đồ uống, Combo, Buffet) và ô tìm kiếm nhanh theo Tên, Mã món hoặc Barcode.' },
    { step: '2. Tùy chọn Modifiers (Yêu cầu thêm):', desc: 'Khi chọn món, hệ thống hiển thị bảng tùy chọn kích thước (Size S/M/L) và mức đường/đá kèm ghi chú bếp (ví dụ: "Không hành", "Ít cay").' },
    { step: '3. Giỏ hàng & Số lượng:', desc: 'Cột giỏ hàng bên trái tự động tính tiền và lưu trữ tạm thời các món đã chọn. Nhân viên có thể tăng giảm số lượng trực tiếp.' },
    { step: '4. Gửi yêu cầu đến Bếp/Bar:', desc: 'Bấm nút "GỬI YÊU CẦU ĐẾN BẾP / BAR". Các món sẽ ngay lập tức xuất hiện trên màn hình KDS nhà bếp.' }
];

let posY = 140;
posSteps.forEach(p => {
    doc.font('Arial-Bold').fillColor('#10b981').text(p.step, 50, posY);
    doc.font('Arial').fillColor('#334155').text(p.desc, 70, posY + 15, { width: 475, lineGap: 3 });
    posY += 60;
});

// ==========================================
// 6. PAGE 6: KITCHEN DISPLAY SYSTEM (KDS)
// ==========================================
doc.addPage();
doc.font('Arial-Bold').fillColor('#0f172a').fontSize(16).text('CHƯƠNG 4: MÀN HÌNH BẾP & BAR (KDS)', 50, 50);
drawHR(70);

doc.font('Arial').fillColor('#334155').fontSize(11).text(
    'Hệ thống màn hình bếp KDS giúp loại bỏ hóa đơn giấy truyền thống, tự động hóa luồng chế biến món ăn và đảm bảo tốc độ phục vụ.',
    50, 90, { width: 495 }
);

// KDS Columns
doc.font('Arial-Bold').fillColor('#0f172a').fontSize(12).text('Quy trình 3 bước trên KDS:', 50, 140);

const kdsSteps = [
    { title: 'Bước 1: Chờ Chế Biến', desc: 'Món ăn mới order được đẩy xuống sẽ hiển thị ở cột này kèm số bàn và ghi chú cụ thể. Bộ đếm thời gian bắt đầu chạy.' },
    { title: 'Bước 2: Đang Chế Biến', desc: 'Đầu bếp bấm "Bắt đầu chế biến" để chuyển món sang cột giữa. Nhân viên chạy bàn có thể nhìn trạng thái để biết bếp đã nhận làm.' },
    { title: 'Bước 3: Hoàn thành & Ra Món', desc: 'Khi nấu xong, bếp bấm "Báo ra món". Hệ thống sẽ gửi thông báo và chuyển sang cột Đã Xong để nhân viên phục vụ bê lên bàn cho khách.' }
];

let kdsY = 170;
kdsSteps.forEach(k => {
    doc.font('Arial-Bold').fillColor('#10b981').text(k.title, 60, kdsY);
    doc.font('Arial').fillColor('#334155').text(k.desc, 60, kdsY + 15, { width: 475 });
    kdsY += 55;
});

// Kitchen Bar split
doc.rect(50, 360, 495, 100).fill('#eff6ff').stroke('#bfdbfe');
doc.font('Arial-Bold').fillColor('#1e40af').fontSize(12).text('Phân tách khu vực Bếp / Bar:', 70, 380);
doc.font('Arial').fillColor('#1e3a8a').fontSize(11).text(
    'Màn hình KDS tự động nhận diện danh mục món để phân loại: Món ăn sẽ chuyển vào màn hình Bếp ăn, đồ uống sẽ chuyển riêng vào quầy Pha chế (Bar) giúp nâng cao hiệu suất làm việc chuyên môn.',
    70, 400, { width: 455, lineGap: 3 }
);

// ==========================================
// 7. PAGE 7: CHECKOUT & VIETQR
// ==========================================
doc.addPage();
doc.font('Arial-Bold').fillColor('#0f172a').fontSize(16).text('CHƯƠNG 5: THANH TOÁN & MBBANK VIETQR', 50, 50);
drawHR(70);

doc.font('Arial').fillColor('#334155').fontSize(11).text(
    'Phân hệ thanh toán hỗ trợ quy trình khép kín, tự động tính thuế VAT, giảm trừ voucher khuyến mãi và xuất mã QR thanh toán.',
    50, 90, { width: 495 }
);

const checkoutFlow = [
    { step: '1. Khấu trừ Voucher:', desc: 'Nhập mã voucher (ví dụ: "GIAM20K") vào ô khuyến mãi để giảm trực tiếp số tiền phải trả.' },
    { step: '2. Cổng MBBank VietQR tự động:', desc: 'Khi chọn phương thức "Chuyển khoản (QR)", hệ thống kết nối API ngân hàng để sinh mã VietQR chứa đúng số tiền cần thanh toán và nội dung chuyển khoản theo số bàn.' },
    { step: '3. In Hóa Đơn Nhiệt (Bill):', desc: 'Bấm nút "HOÀN TẤT & IN HÓA ĐƠN" để in phiếu thanh toán khổ 80mm chuyên nghiệp hiển thị đầy đủ tên món, chiết khấu, thuế, và barcode hóa đơn.' },
    { step: '4. Tự động Trừ Kho & Ghi Sổ Quỹ:', desc: 'Sau khi thanh toán thành công, hệ thống tự động trừ tồn kho nguyên liệu tương ứng và tạo phiếu thu vào Sổ Quỹ thu chi.' }
];

let payY = 140;
checkoutFlow.forEach(p => {
    doc.font('Arial-Bold').fillColor('#10b981').text(p.step, 50, payY);
    doc.font('Arial').fillColor('#334155').text(p.desc, 70, payY + 15, { width: 475, lineGap: 3 });
    payY += 60;
});

// ==========================================
// 8. PAGE 8: INVENTORY & BOM & AI
// ==========================================
doc.addPage();
doc.font('Arial-Bold').fillColor('#0f172a').fontSize(16).text('CHƯƠNG 6: KHO HÀNG, ĐỊNH LƯỢNG (BOM) & AI', 50, 50);
drawHR(70);

doc.font('Arial').fillColor('#334155').fontSize(11).text(
    'Phân hệ Quản trị Kho hàng nâng cao giúp chủ cửa hàng kiểm soát chặt chẽ nguyên vật liệu đầu vào và các phân tích dự báo AI.',
    50, 90, { width: 495 }
);

// Topics
const invTopics = [
    { name: 'Định Lượng Nguyên Liệu (BOM):', content: 'Thiết lập công thức chế biến cho từng món ăn. Ví dụ: 1 tô Phở Bò cần 150g bánh phở, 80g thịt bò, và 0.5 lít nước dùng. Khi bán 1 tô phở, kho sẽ tự động trừ tương ứng.' },
    { name: 'Quản Lý Nhập Hàng & Nhà Cung Cấp:', content: 'Tạo đơn đặt hàng nhà cung cấp (Purchase Order), ghi nhận công nợ NCC và tạo phiếu chi tự động khi thanh toán tiền hàng.' },
    { name: 'Kiểm Kho Định Kỳ:', content: 'Lập phiếu kiểm kho, so sánh chênh lệch giữa số lượng tồn thực tế và số lượng hệ thống để cân bằng kho.' },
    { name: 'Dự Báo Kinh Doanh Bằng AI:', content: 'Mục "Liên kết & AI" tích hợp mô hình dự báo doanh số ngày mai/tuần tới dựa trên doanh thu lịch sử và gợi ý kế hoạch đặt hàng nguyên liệu phù hợp.' }
];

let invY = 140;
invTopics.forEach(t => {
    doc.font('Arial-Bold').fillColor('#0f172a').text(t.name, 50, invY);
    doc.font('Arial').fillColor('#334155').text(t.content, 50, invY + 15, { width: 495, lineGap: 3 });
    invY += 65;
});

// Final line
doc.strokeColor('#10b981').lineWidth(3).moveTo(50, 720).lineTo(545, 720).stroke();
doc.font('Arial-Bold').fillColor('#10b981').fontSize(12).text('HẾT TÀI LIỆU HƯỚNG DẪN', 50, 735, { align: 'center' });

// End document
doc.end();

writeStream.on('finish', () => {
    console.log('PDF Manual generated successfully at: ' + outputPath);
});
