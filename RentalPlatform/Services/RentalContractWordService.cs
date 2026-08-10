using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using RentalPlatform.Models.Entities;

namespace RentalPlatform.Services;

public record RentalContractDocument(byte[] Content, string FileName);

public interface IRentalContractWordService
{
    RentalContractDocument Generate(RentalOrder order);
}

/// <summary>
/// Tạo DOCX thuần OOXML bằng thư viện có sẵn của .NET, không phụ thuộc Microsoft Word
/// hoặc package thương mại trên máy chủ.
/// </summary>
public sealed class RentalContractWordService : IRentalContractWordService
{
    private const string WordNs = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public RentalContractDocument Generate(RentalOrder order)
    {
        if (order.Contract is null) throw new InvalidOperationException("Đơn thuê chưa có hợp đồng.");

        using var output = new MemoryStream();
        using (var package = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddXml(package, "[Content_Types].xml", WriteContentTypes);
            AddXml(package, "_rels/.rels", WriteRootRelationships);
            AddXml(package, "docProps/core.xml", writer => WriteCoreProperties(writer, order));
            AddXml(package, "docProps/app.xml", WriteAppProperties);
            AddXml(package, "word/styles.xml", WriteStyles);
            AddXml(package, "word/settings.xml", WriteSettings);
            AddXml(package, "word/_rels/document.xml.rels", WriteDocumentRelationships);
            AddXml(package, "word/header1.xml", writer => WriteHeader(writer, order.Contract.SoHopDong));
            AddXml(package, "word/footer1.xml", WriteFooter);
            AddXml(package, "word/document.xml", writer => WriteDocument(writer, order));
        }

        var safeNumber = string.Concat(order.Contract.SoHopDong.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));
        return new RentalContractDocument(output.ToArray(), $"Hop-dong-thue-xe-{safeNumber}.docx");
    }

    private static void AddXml(ZipArchive package, string path, Action<XmlWriter> write)
    {
        var entry = package.CreateEntry(path, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false,
            CloseOutput = false
        });
        write(writer);
    }

    private static void WriteContentTypes(XmlWriter x)
    {
        x.WriteStartDocument();
        x.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");
        Empty(x, "Default", ("Extension", "rels"), ("ContentType", "application/vnd.openxmlformats-package.relationships+xml"));
        Empty(x, "Default", ("Extension", "xml"), ("ContentType", "application/xml"));
        Empty(x, "Override", ("PartName", "/word/document.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"));
        Empty(x, "Override", ("PartName", "/word/styles.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"));
        Empty(x, "Override", ("PartName", "/word/settings.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml"));
        Empty(x, "Override", ("PartName", "/word/header1.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml"));
        Empty(x, "Override", ("PartName", "/word/footer1.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml"));
        Empty(x, "Override", ("PartName", "/docProps/core.xml"), ("ContentType", "application/vnd.openxmlformats-package.core-properties+xml"));
        Empty(x, "Override", ("PartName", "/docProps/app.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.extended-properties+xml"));
        x.WriteEndElement();
        x.WriteEndDocument();
    }

    private static void WriteRootRelationships(XmlWriter x)
    {
        x.WriteStartDocument();
        x.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
        Empty(x, "Relationship", ("Id", "rId1"), ("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"), ("Target", "word/document.xml"));
        Empty(x, "Relationship", ("Id", "rId2"), ("Type", "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties"), ("Target", "docProps/core.xml"));
        Empty(x, "Relationship", ("Id", "rId3"), ("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties"), ("Target", "docProps/app.xml"));
        x.WriteEndElement();
        x.WriteEndDocument();
    }

    private static void WriteDocumentRelationships(XmlWriter x)
    {
        x.WriteStartDocument();
        x.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
        Empty(x, "Relationship", ("Id", "rId1"), ("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"), ("Target", "styles.xml"));
        Empty(x, "Relationship", ("Id", "rId2"), ("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/settings"), ("Target", "settings.xml"));
        Empty(x, "Relationship", ("Id", "rId3"), ("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/header"), ("Target", "header1.xml"));
        Empty(x, "Relationship", ("Id", "rId4"), ("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer"), ("Target", "footer1.xml"));
        x.WriteEndElement();
        x.WriteEndDocument();
    }

    private static void WriteCoreProperties(XmlWriter x, RentalOrder order)
    {
        var created = ToVietnamTime(order.CreatedAt).ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture);
        x.WriteStartDocument();
        x.WriteStartElement("cp", "coreProperties", "http://schemas.openxmlformats.org/package/2006/metadata/core-properties");
        x.WriteAttributeString("xmlns", "dc", null, "http://purl.org/dc/elements/1.1/");
        x.WriteAttributeString("xmlns", "dcterms", null, "http://purl.org/dc/terms/");
        x.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
        Element(x, "dc", "title", "http://purl.org/dc/elements/1.1/", $"Hợp đồng thuê xe {order.Contract!.SoHopDong}");
        Element(x, "dc", "creator", "http://purl.org/dc/elements/1.1/", "RentalPlatform");
        Element(x, "cp", "lastModifiedBy", "http://schemas.openxmlformats.org/package/2006/metadata/core-properties", "RentalPlatform");
        x.WriteStartElement("dcterms", "created", "http://purl.org/dc/terms/");
        x.WriteAttributeString("xsi", "type", "http://www.w3.org/2001/XMLSchema-instance", "dcterms:W3CDTF");
        x.WriteString(created);
        x.WriteEndElement();
        x.WriteEndElement();
        x.WriteEndDocument();
    }

    private static void WriteAppProperties(XmlWriter x)
    {
        x.WriteStartDocument();
        x.WriteStartElement("Properties", "http://schemas.openxmlformats.org/officeDocument/2006/extended-properties");
        x.WriteAttributeString("xmlns", "vt", null, "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes");
        Element(x, "Application", "RentalPlatform");
        Element(x, "AppVersion", "1.0");
        x.WriteEndElement();
        x.WriteEndDocument();
    }

    private static void WriteSettings(XmlWriter x)
    {
        x.WriteStartDocument();
        x.WriteStartElement("w", "settings", WordNs);
        EmptyW(x, "updateFields", ("val", "true"));
        EmptyW(x, "defaultTabStop", ("val", "720"));
        x.WriteEndElement();
        x.WriteEndDocument();
    }

    private static void WriteStyles(XmlWriter x)
    {
        x.WriteStartDocument();
        x.WriteStartElement("w", "styles", WordNs);
        WriteStyle(x, "Normal", "Normal", 26, bold: false, before: 0, after: 120, line: 300);
        WriteStyle(x, "Title", "Tiêu đề hợp đồng", 34, bold: true, before: 0, after: 120, line: 300, centered: true);
        WriteStyle(x, "Subtitle", "Phụ đề", 26, bold: true, before: 0, after: 80, line: 280, centered: true);
        WriteStyle(x, "Heading1", "Điều khoản", 27, bold: true, before: 240, after: 100, line: 280, keepNext: true);
        WriteStyle(x, "Small", "Chữ nhỏ", 22, bold: false, before: 0, after: 60, line: 260);
        x.WriteEndElement();
        x.WriteEndDocument();
    }

    private static void WriteStyle(XmlWriter x, string id, string name, int halfPoints, bool bold, int before, int after, int line, bool centered = false, bool keepNext = false)
    {
        x.WriteStartElement("w", "style", WordNs);
        x.WriteAttributeString("w", "type", WordNs, "paragraph");
        x.WriteAttributeString("w", "styleId", WordNs, id);
        EmptyW(x, "name", ("val", name));
        if (id == "Normal") EmptyW(x, "qFormat");
        x.WriteStartElement("w", "pPr", WordNs);
        if (keepNext) EmptyW(x, "keepNext");
        EmptyW(x, "spacing", ("before", before.ToString()), ("after", after.ToString()), ("line", line.ToString()), ("lineRule", "auto"));
        if (centered) EmptyW(x, "jc", ("val", "center"));
        x.WriteEndElement();
        x.WriteStartElement("w", "rPr", WordNs);
        WriteRunFonts(x);
        EmptyW(x, "sz", ("val", halfPoints.ToString()));
        EmptyW(x, "szCs", ("val", halfPoints.ToString()));
        if (bold) EmptyW(x, "b");
        x.WriteEndElement();
        x.WriteEndElement();
    }

    private static void WriteHeader(XmlWriter x, string contractNumber)
    {
        x.WriteStartDocument();
        x.WriteStartElement("w", "hdr", WordNs);
        Paragraph(x, $"HỢP ĐỒNG THUÊ XE · {contractNumber}", style: "Small", align: "right", color: "666666");
        x.WriteEndElement();
        x.WriteEndDocument();
    }

    private static void WriteFooter(XmlWriter x)
    {
        x.WriteStartDocument();
        x.WriteStartElement("w", "ftr", WordNs);
        x.WriteStartElement("w", "p", WordNs);
        x.WriteStartElement("w", "pPr", WordNs);
        EmptyW(x, "jc", ("val", "center"));
        x.WriteEndElement();
        Run(x, "RentalPlatform · Trang ", size: 20, color: "777777");
        Field(x, "PAGE", "1");
        Run(x, " / ", size: 20, color: "777777");
        Field(x, "NUMPAGES", "1");
        x.WriteEndElement();
        x.WriteEndElement();
        x.WriteEndDocument();
    }

    private static void WriteDocument(XmlWriter x, RentalOrder order)
    {
        var contract = order.Contract!;
        var created = ToVietnamTime(contract.CreatedAt);
        var ownerName = ValueOr(contract.HoTenChuXe, order.Owner?.HoTen);
        var ownerEmail = ValueOr(contract.EmailChuXe, order.Owner?.Email);
        var ownerPhone = ValueOr(contract.SDTChuXe, order.Owner?.SDT);
        var ownerId = ValueOr(contract.SoCCCDChuXe, order.Owner?.SoCCCD);
        var ownerAddress = ValueOr(contract.DiaChiChuXe, order.Owner?.DiaChi);

        x.WriteStartDocument();
        x.WriteStartElement("w", "document", WordNs);
        x.WriteAttributeString("xmlns", "r", null, RelNs);
        x.WriteStartElement("w", "body", WordNs);

        Paragraph(x, "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM", bold: true, align: "center", after: 0);
        Paragraph(x, "Độc lập - Tự do - Hạnh phúc", bold: true, align: "center", after: 160, bottomBorder: true);
        Paragraph(x, "HỢP ĐỒNG THUÊ XE", style: "Title");
        Paragraph(x, $"Số: {contract.SoHopDong}", style: "Subtitle");
        Paragraph(x, $"Mã đơn thuê: {order.MaDon}", align: "center", italic: true, after: 200);
        Paragraph(x, $"Hôm nay, ngày {created:dd} tháng {created:MM} năm {created:yyyy}, các bên tự nguyện thỏa thuận giao kết hợp đồng thuê xe với nội dung sau:", justify: true);

        Heading(x, "CĂN CỨ GIAO KẾT");
        LeadParagraph(x, "Căn cứ pháp luật.", " Bộ luật Dân sự số 91/2015/QH13, đặc biệt các quy định về hợp đồng thuê tài sản; Luật Giao dịch điện tử số 20/2023/QH15 đối với việc giao kết và lưu trữ hợp đồng bằng phương tiện điện tử.");
        LeadParagraph(x, "Căn cứ thỏa thuận.", " Nhu cầu thuê xe của Bên B, khả năng cho thuê của Bên A và sự tự nguyện của hai bên.");

        Heading(x, "THÔNG TIN CÁC BÊN");
        PartyTable(x,
            ("BÊN CHO THUÊ (BÊN A)", ownerName, ownerId, ownerPhone, ownerEmail, ownerAddress, null),
            ("BÊN THUÊ (BÊN B)", order.HoTenKhach, order.SoCCCD, order.SDTKhach, order.EmailKhach, order.DiaChiKhach, order.SoGPLX));
        Paragraph(x, "Bên A và Bên B sau đây gọi riêng là “Bên” và gọi chung là “Các Bên”.", italic: true, after: 140);

        Heading(x, "ĐIỀU 1. ĐỐI TƯỢNG VÀ THỜI HẠN THUÊ");
        Paragraph(x, "Bên A đồng ý cho Bên B thuê các xe dưới đây để sử dụng đúng mục đích, trong thời gian và với tình trạng đã được ghi nhận:", justify: true);
        VehicleTable(x, order.Items);

        Heading(x, "ĐIỀU 2. GIÁ THUÊ VÀ THANH TOÁN");
        LeadParagraph(x, "Tổng giá trị hợp đồng.", $" {order.TongTien.ToString("N0", ViCulture)} đồng (Bằng chữ: {MoneyToWords(order.TongTien)}). Trong đó tiền thuê {order.TienThue.ToString("N0", ViCulture)} đồng, tiền đặt cọc {order.TienDatCoc.ToString("N0", ViCulture)} đồng và phí dịch vụ hệ thống {order.PhiDichVu.ToString("N0", ViCulture)} đồng.");
        var paymentMethod = order.PhuongThucThanhToan switch
        {
            PhuongThucThanhToanDon.ChuyenKhoan => "Chuyển khoản",
            PhuongThucThanhToanDon.ThanhToanKhiNhanXe => "Thanh toán khi nhận xe",
            PhuongThucThanhToanDon.ViDienTu => "Ví điện tử",
            PhuongThucThanhToanDon.TheThanhToan => "Thẻ thanh toán",
            _ => "Chưa lựa chọn"
        };
        LeadParagraph(x, "Phương thức.", $" {paymentMethod}. Trạng thái tại thời điểm xuất file: {PaymentStatus(order.TrangThaiThanhToan)}. Nội dung và mã tham chiếu thanh toán (nếu có): {ValueOr(order.MaThamChieuThanhToan)}.");
        LeadParagraph(x, "Chi phí ngoài giá thuê.", " Nhiên liệu, phí cầu đường, bến bãi, tiền phạt vi phạm giao thông và các chi phí phát sinh do việc sử dụng xe của Bên B không nằm trong giá thuê, trừ khi Các Bên có thỏa thuận khác bằng văn bản.");

        Heading(x, "ĐIỀU 3. BÀN GIAO, SỬ DỤNG VÀ HOÀN TRẢ XE");
        LeadParagraph(x, "Bàn giao.", " Bên A giao đúng xe, giấy tờ và phụ kiện đã thỏa thuận; hai bên kiểm tra, chụp ảnh và xác nhận tình trạng thực tế trong Phụ lục bàn giao kèm hợp đồng.");
        LeadParagraph(x, "Sử dụng.", " Bên B phải có giấy phép lái xe phù hợp; tuân thủ pháp luật giao thông; không dùng xe cho mục đích trái pháp luật, đua xe, cầm cố, thế chấp, cho thuê lại hoặc giao người không đủ điều kiện điều khiển khi chưa có chấp thuận của Bên A.");
        LeadParagraph(x, "Hoàn trả.", " Bên B trả xe đúng thời gian, địa điểm và tình trạng khi nhận, trừ hao mòn tự nhiên. Mọi thay đổi thời gian thuê phải được Bên A chấp thuận và cập nhật có lưu vết.");

        Heading(x, "ĐIỀU 4. QUYỀN VÀ NGHĨA VỤ CỦA BÊN A");
        LeadParagraph(x, "Cam kết về xe.", " Bên A cam kết có quyền cho thuê hợp pháp; xe đủ điều kiện lưu hành, thông tin xe và tình trạng công bố là trung thực tại thời điểm bàn giao.");
        LeadParagraph(x, "Nghĩa vụ hỗ trợ.", " Bên A cung cấp thông tin cần thiết để vận hành an toàn, phối hợp xử lý sự cố không do lỗi của Bên B và nhận lại xe theo thỏa thuận.");
        LeadParagraph(x, "Quyền kiểm tra.", " Bên A có quyền kiểm tra tình trạng xe trong phạm vi hợp lý và yêu cầu Bên B dừng sử dụng khi có dấu hiệu mất an toàn hoặc vi phạm hợp đồng.");

        Heading(x, "ĐIỀU 5. QUYỀN VÀ NGHĨA VỤ CỦA BÊN B");
        LeadParagraph(x, "Kiểm tra khi nhận.", " Bên B kiểm tra xe, giấy tờ và phụ kiện; phản ánh ngay sai lệch so với mô tả trước khi ký biên bản bàn giao.");
        LeadParagraph(x, "Bảo quản và thông báo.", " Bên B bảo quản xe, không tự ý sửa chữa hoặc thay đổi kết cấu; thông báo ngay cho Bên A và cơ quan có thẩm quyền khi xảy ra tai nạn, mất cắp, hư hỏng hoặc sự cố.");
        LeadParagraph(x, "Trách nhiệm tài chính.", " Bên B thanh toán đủ, đúng hạn và chịu chi phí khắc phục thiệt hại thực tế do lỗi của mình; mức bồi thường được xác định theo chứng từ, kết quả giám định hoặc thỏa thuận hợp pháp của Các Bên.");

        Heading(x, "ĐIỀU 6. VI PHẠM, HỦY HỢP ĐỒNG VÀ BỒI THƯỜNG");
        Paragraph(x, "Bên vi phạm nghĩa vụ phải khắc phục hậu quả và bồi thường thiệt hại thực tế, trực tiếp, hợp lý theo quy định pháp luật. Việc hủy đơn, hoàn tiền hoặc khấu trừ chi phí căn cứ vào trạng thái thanh toán, mức độ thực hiện hợp đồng, bằng chứng thiệt hại và thỏa thuận có lưu vết giữa Các Bên. Hợp đồng có thể chấm dứt trước hạn khi Các Bên thống nhất hoặc khi một Bên vi phạm nghiêm trọng nghĩa vụ làm mục đích thuê không đạt được.", justify: true);

        Heading(x, "ĐIỀU 7. SỰ KIỆN BẤT KHẢ KHÁNG");
        Paragraph(x, "Bên bị ảnh hưởng bởi sự kiện khách quan không thể lường trước và không thể khắc phục dù đã áp dụng biện pháp cần thiết phải thông báo cho Bên còn lại sớm nhất có thể, cung cấp bằng chứng phù hợp và chủ động hạn chế thiệt hại. Các Bên ưu tiên thỏa thuận điều chỉnh thời gian, nghĩa vụ hoặc chấm dứt hợp đồng tùy mức độ ảnh hưởng.", justify: true);

        Heading(x, "ĐIỀU 8. DỮ LIỆU, GIAO KẾT ĐIỆN TỬ VÀ BẢO MẬT");
        Paragraph(x, "Các Bên đồng ý sử dụng tài khoản trên RentalPlatform để xác nhận giao kết. Hệ thống lưu mã đơn, nội dung hợp đồng, tài khoản xác nhận, thời điểm ký và giao dịch liên quan. File Word là bản thể hiện có thể tải xuống của dữ liệu hợp đồng; giá trị chứng cứ còn phụ thuộc tính toàn vẹn, khả năng truy cập và phương thức xác định người ký theo pháp luật áp dụng. Thông tin cá nhân chỉ được sử dụng cho việc giao kết, thực hiện hợp đồng, thanh toán, giải quyết sự cố và nghĩa vụ pháp lý liên quan.", justify: true);

        Heading(x, "ĐIỀU 9. GIẢI QUYẾT TRANH CHẤP");
        Paragraph(x, "Tranh chấp trước hết được giải quyết bằng thương lượng và đối chiếu dữ liệu đơn thuê, thanh toán, bàn giao và trao đổi có lưu vết. Nếu không đạt thỏa thuận trong thời hạn hợp lý, mỗi Bên có quyền yêu cầu cơ quan có thẩm quyền giải quyết theo pháp luật Việt Nam.", justify: true);

        Heading(x, "ĐIỀU 10. HIỆU LỰC HỢP ĐỒNG");
        Paragraph(x, "Hợp đồng có hiệu lực khi cả Bên A và Bên B hoàn tất xác nhận ký điện tử trên hệ thống, trừ khi Các Bên có thỏa thuận khác. Hợp đồng, phụ lục bàn giao và dữ liệu giao dịch liên quan tạo thành một bộ thống nhất. Các Bên xác nhận đã đọc, hiểu rõ quyền và nghĩa vụ, thông tin cung cấp là đúng sự thật và tự nguyện chấp thuận toàn bộ nội dung.", justify: true);

        SignatureTable(x, order, ownerName);

        PageBreak(x);
        Paragraph(x, "PHỤ LỤC 01", style: "Title");
        Paragraph(x, "BIÊN BẢN BÀN GIAO VÀ HOÀN TRẢ XE", style: "Subtitle");
        Paragraph(x, $"Kèm theo Hợp đồng số {contract.SoHopDong}", align: "center", italic: true, after: 180);
        Paragraph(x, "Phụ lục được Các Bên hoàn thiện tại thời điểm giao và nhận lại xe. Ảnh hiện trạng, biên bản sự cố và chứng từ liên quan (nếu có) là bộ phận kèm theo.", justify: true);
        HandoverTable(x, order.Items);
        Heading(x, "XÁC NHẬN CHUNG");
        FormLine(x, "Địa điểm bàn giao");
        FormLine(x, "Số km trên đồng hồ khi giao / khi trả");
        FormLine(x, "Mức nhiên liệu hoặc dung lượng pin khi giao / khi trả");
        FormLine(x, "Giấy tờ, chìa khóa và phụ kiện đi kèm");
        FormLine(x, "Hư hỏng hoặc ghi nhận bổ sung");
        SignatureTable(x, order, ownerName, handover: true);

        WriteSectionProperties(x);
        x.WriteEndElement();
        x.WriteEndElement();
        x.WriteEndDocument();
    }

    private static void PartyTable(XmlWriter x,
        (string Title, string Name, string Id, string Phone, string Email, string Address, string? License) a,
        (string Title, string Name, string Id, string Phone, string Email, string Address, string? License) b)
    {
        StartTable(x, 9360, 120, new[] { 4680, 4680 });
        StartRow(x, repeatHeader: false);
        PartyCell(x, a);
        PartyCell(x, b);
        EndRow(x);
        EndTable(x);
    }

    private static void PartyCell(XmlWriter x, (string Title, string Name, string Id, string Phone, string Email, string Address, string? License) p)
    {
        StartCell(x, 4680, fill: "F2F4F7", vertical: "top");
        Paragraph(x, p.Title, bold: true, align: "center", after: 100);
        LabelValue(x, "Họ và tên", ValueOr(p.Name));
        LabelValue(x, "CCCD/CMND", ValueOr(p.Id));
        if (!string.IsNullOrWhiteSpace(p.License)) LabelValue(x, "Giấy phép lái xe", p.License!);
        LabelValue(x, "Điện thoại", ValueOr(p.Phone));
        LabelValue(x, "Email", ValueOr(p.Email));
        LabelValue(x, "Địa chỉ", ValueOr(p.Address));
        EndCell(x);
    }

    private static void VehicleTable(XmlWriter x, IEnumerable<RentalOrderItem> items)
    {
        var widths = new[] { 500, 2160, 1220, 1760, 1760, 760, 1200 };
        StartTable(x, 9360, 120, widths);
        StartRow(x, repeatHeader: true);
        HeaderCell(x, "STT", widths[0]);
        HeaderCell(x, "Xe / biển số", widths[1]);
        HeaderCell(x, "Loại xe", widths[2]);
        HeaderCell(x, "Nhận xe", widths[3]);
        HeaderCell(x, "Trả xe", widths[4]);
        HeaderCell(x, "Số ngày", widths[5]);
        HeaderCell(x, "Thành tiền", widths[6]);
        EndRow(x);
        var index = 1;
        foreach (var item in items)
        {
            StartRow(x);
            TextCell(x, index++.ToString(), widths[0], align: "center");
            TextCell(x, $"{item.TenXe}\n{item.BienSo}", widths[1]);
            TextCell(x, item.LoaiXe, widths[2], align: "center");
            TextCell(x, item.NgayNhanXe.ToString("dd/MM/yyyy\nHH:mm"), widths[3], align: "center");
            TextCell(x, item.NgayTraXe.ToString("dd/MM/yyyy\nHH:mm"), widths[4], align: "center");
            TextCell(x, item.SoNgayThue.ToString(), widths[5], align: "center");
            TextCell(x, item.ThanhTien.ToString("N0", ViCulture) + " đ", widths[6], align: "right");
            EndRow(x);
            StartRow(x);
            TextCell(x, "Tình trạng", widths[0] + widths[1], bold: true);
            TextCell(x, $"{item.TinhTrangLucDat}. Đơn giá: {item.DonGiaNgay.ToString("N0", ViCulture)} đ/ngày.", widths.Skip(2).Sum(), colspan: 5);
            EndRow(x);
        }
        EndTable(x);
    }

    private static void HandoverTable(XmlWriter x, IEnumerable<RentalOrderItem> items)
    {
        var widths = new[] { 500, 2000, 1150, 2450, 1630, 1630 };
        StartTable(x, 9360, 120, widths);
        StartRow(x, repeatHeader: true);
        HeaderCell(x, "STT", widths[0]);
        HeaderCell(x, "Xe", widths[1]);
        HeaderCell(x, "Biển số", widths[2]);
        HeaderCell(x, "Tình trạng lúc đặt", widths[3]);
        HeaderCell(x, "Khi giao", widths[4]);
        HeaderCell(x, "Khi trả", widths[5]);
        EndRow(x);
        var index = 1;
        foreach (var item in items)
        {
            StartRow(x);
            TextCell(x, index++.ToString(), widths[0], align: "center");
            TextCell(x, item.TenXe, widths[1]);
            TextCell(x, item.BienSo, widths[2], align: "center");
            TextCell(x, item.TinhTrangLucDat, widths[3]);
            TextCell(x, "Đạt / Ghi chú: __________________", widths[4]);
            TextCell(x, "Đạt / Ghi chú: __________________", widths[5]);
            EndRow(x);
        }
        EndTable(x);
    }

    private static void SignatureTable(XmlWriter x, RentalOrder order, string ownerName, bool handover = false)
    {
        Paragraph(x, string.Empty, after: 80);
        StartTable(x, 9360, 120, new[] { 4680, 4680 }, borders: false);
        StartRow(x);
        SignatureCell(x, "BÊN CHO THUÊ (BÊN A)", ownerName,
            handover ? false : order.Contract!.DaKyChuXe,
            handover ? null : order.Contract!.NgayKyChuXe);
        SignatureCell(x, "BÊN THUÊ (BÊN B)", order.HoTenKhach,
            handover ? false : order.Contract!.DaKyKhach,
            handover ? null : order.Contract!.NgayKyKhach);
        EndRow(x);
        EndTable(x);
    }

    private static void SignatureCell(XmlWriter x, string title, string name, bool signed, DateTime? signedAt)
    {
        StartCell(x, 4680, borders: false, vertical: "top");
        Paragraph(x, title, bold: true, align: "center", after: 20);
        Paragraph(x, "(Ký và ghi rõ họ tên)", italic: true, align: "center", after: 120);
        if (signed)
        {
            Paragraph(x, "ĐÃ XÁC NHẬN KÝ ĐIỆN TỬ", bold: true, align: "center", color: "1F6F52", after: 20);
            Paragraph(x, signedAt.HasValue ? $"Thời điểm: {ToVietnamTime(signedAt.Value):dd/MM/yyyy HH:mm:ss}" : "", align: "center", after: 220);
        }
        else
        {
            Paragraph(x, "CHƯA KÝ / KÝ TRỰC TIẾP TẠI ĐÂY", bold: true, align: "center", color: "8A5A00", after: 260);
        }
        Paragraph(x, name, bold: true, align: "center");
        EndCell(x);
    }

    private static void Heading(XmlWriter x, string text) => Paragraph(x, text, style: "Heading1");

    private static void LeadParagraph(XmlWriter x, string lead, string body)
    {
        x.WriteStartElement("w", "p", WordNs);
        x.WriteStartElement("w", "pPr", WordNs);
        EmptyW(x, "pStyle", ("val", "Normal"));
        EmptyW(x, "jc", ("val", "both"));
        x.WriteEndElement();
        Run(x, lead, bold: true);
        Run(x, body);
        x.WriteEndElement();
    }

    private static void LabelValue(XmlWriter x, string label, string value)
    {
        x.WriteStartElement("w", "p", WordNs);
        x.WriteStartElement("w", "pPr", WordNs);
        EmptyW(x, "spacing", ("after", "50"), ("line", "280"), ("lineRule", "auto"));
        x.WriteEndElement();
        Run(x, label + ": ", bold: true, size: 24);
        Run(x, value, size: 24);
        x.WriteEndElement();
    }

    private static void FormLine(XmlWriter x, string label)
        => Paragraph(x, $"{label}: __________________________________________________________________________", after: 120);

    private static void Paragraph(XmlWriter x, string text, string style = "Normal", bool bold = false, bool italic = false,
        string? align = null, bool justify = false, int? after = null, string? color = null, bool bottomBorder = false)
    {
        x.WriteStartElement("w", "p", WordNs);
        x.WriteStartElement("w", "pPr", WordNs);
        EmptyW(x, "pStyle", ("val", style));
        if (align is not null) EmptyW(x, "jc", ("val", align));
        else if (justify) EmptyW(x, "jc", ("val", "both"));
        if (after.HasValue) EmptyW(x, "spacing", ("after", after.Value.ToString()), ("line", "300"), ("lineRule", "auto"));
        if (bottomBorder)
        {
            x.WriteStartElement("w", "pBdr", WordNs);
            EmptyW(x, "bottom", ("val", "single"), ("sz", "6"), ("space", "4"), ("color", "000000"));
            x.WriteEndElement();
        }
        x.WriteEndElement();
        Run(x, text, bold, italic, color: color);
        x.WriteEndElement();
    }

    private static void Run(XmlWriter x, string text, bool bold = false, bool italic = false, int? size = null, string? color = null)
    {
        x.WriteStartElement("w", "r", WordNs);
        x.WriteStartElement("w", "rPr", WordNs);
        WriteRunFonts(x);
        if (bold) EmptyW(x, "b");
        if (italic) EmptyW(x, "i");
        if (size.HasValue)
        {
            EmptyW(x, "sz", ("val", size.Value.ToString()));
            EmptyW(x, "szCs", ("val", size.Value.ToString()));
        }
        if (color is not null) EmptyW(x, "color", ("val", color));
        x.WriteEndElement();
        x.WriteStartElement("w", "t", WordNs);
        if (text.StartsWith(' ') || text.EndsWith(' ')) x.WriteAttributeString("xml", "space", null, "preserve");
        x.WriteString(text);
        x.WriteEndElement();
        x.WriteEndElement();
    }

    private static void Field(XmlWriter x, string instruction, string fallback)
    {
        x.WriteStartElement("w", "r", WordNs);
        EmptyW(x, "fldChar", ("fldCharType", "begin"));
        x.WriteEndElement();
        x.WriteStartElement("w", "r", WordNs);
        x.WriteStartElement("w", "instrText", WordNs);
        x.WriteAttributeString("xml", "space", null, "preserve");
        x.WriteString(" " + instruction + " ");
        x.WriteEndElement();
        x.WriteEndElement();
        x.WriteStartElement("w", "r", WordNs);
        EmptyW(x, "fldChar", ("fldCharType", "separate"));
        x.WriteEndElement();
        Run(x, fallback, size: 20, color: "777777");
        x.WriteStartElement("w", "r", WordNs);
        EmptyW(x, "fldChar", ("fldCharType", "end"));
        x.WriteEndElement();
    }

    private static void PageBreak(XmlWriter x)
    {
        x.WriteStartElement("w", "p", WordNs);
        x.WriteStartElement("w", "r", WordNs);
        EmptyW(x, "br", ("type", "page"));
        x.WriteEndElement();
        x.WriteEndElement();
    }

    private static void StartTable(XmlWriter x, int width, int indent, int[] columns, bool borders = true)
    {
        x.WriteStartElement("w", "tbl", WordNs);
        x.WriteStartElement("w", "tblPr", WordNs);
        EmptyW(x, "tblW", ("w", width.ToString()), ("type", "dxa"));
        EmptyW(x, "tblInd", ("w", indent.ToString()), ("type", "dxa"));
        EmptyW(x, "tblLayout", ("type", "fixed"));
        if (borders)
        {
            x.WriteStartElement("w", "tblBorders", WordNs);
            foreach (var edge in new[] { "top", "left", "bottom", "right", "insideH", "insideV" })
                EmptyW(x, edge, ("val", "single"), ("sz", "4"), ("space", "0"), ("color", "B7BCC5"));
            x.WriteEndElement();
        }
        x.WriteStartElement("w", "tblCellMar", WordNs);
        foreach (var edge in new[] { "top", "bottom" }) EmptyW(x, edge, ("w", "100"), ("type", "dxa"));
        foreach (var edge in new[] { "left", "right" }) EmptyW(x, edge, ("w", "120"), ("type", "dxa"));
        x.WriteEndElement();
        x.WriteEndElement();
        x.WriteStartElement("w", "tblGrid", WordNs);
        foreach (var column in columns) EmptyW(x, "gridCol", ("w", column.ToString()));
        x.WriteEndElement();
    }

    private static void EndTable(XmlWriter x) => x.WriteEndElement();

    private static void StartRow(XmlWriter x, bool repeatHeader = false)
    {
        x.WriteStartElement("w", "tr", WordNs);
        if (repeatHeader)
        {
            x.WriteStartElement("w", "trPr", WordNs);
            EmptyW(x, "tblHeader");
            x.WriteEndElement();
        }
    }

    private static void EndRow(XmlWriter x) => x.WriteEndElement();

    private static void StartCell(XmlWriter x, int width, int colspan = 1, string? fill = null, bool borders = true, string vertical = "center")
    {
        x.WriteStartElement("w", "tc", WordNs);
        x.WriteStartElement("w", "tcPr", WordNs);
        EmptyW(x, "tcW", ("w", width.ToString()), ("type", "dxa"));
        if (colspan > 1) EmptyW(x, "gridSpan", ("val", colspan.ToString()));
        if (fill is not null) EmptyW(x, "shd", ("val", "clear"), ("color", "auto"), ("fill", fill));
        EmptyW(x, "vAlign", ("val", vertical));
        if (!borders)
        {
            x.WriteStartElement("w", "tcBorders", WordNs);
            foreach (var edge in new[] { "top", "left", "bottom", "right", "insideH", "insideV" }) EmptyW(x, edge, ("val", "nil"));
            x.WriteEndElement();
        }
        x.WriteEndElement();
    }

    private static void EndCell(XmlWriter x) => x.WriteEndElement();

    private static void HeaderCell(XmlWriter x, string text, int width)
    {
        StartCell(x, width, fill: "E8EEF5");
        Paragraph(x, text, bold: true, align: "center", after: 20);
        EndCell(x);
    }

    private static void TextCell(XmlWriter x, string text, int width, string? align = null, bool bold = false, int colspan = 1)
    {
        StartCell(x, width, colspan: colspan);
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++) Paragraph(x, lines[i], bold: bold && i == 0, align: align, after: i == lines.Length - 1 ? 20 : 0);
        EndCell(x);
    }

    private static void WriteSectionProperties(XmlWriter x)
    {
        x.WriteStartElement("w", "sectPr", WordNs);
        x.WriteStartElement("w", "headerReference", WordNs);
        x.WriteAttributeString("w", "type", WordNs, "default");
        x.WriteAttributeString("r", "id", RelNs, "rId3");
        x.WriteEndElement();
        x.WriteStartElement("w", "footerReference", WordNs);
        x.WriteAttributeString("w", "type", WordNs, "default");
        x.WriteAttributeString("r", "id", RelNs, "rId4");
        x.WriteEndElement();
        EmptyW(x, "pgSz", ("w", "11906"), ("h", "16838"));
        EmptyW(x, "pgMar", ("top", "1134"), ("right", "1134"), ("bottom", "1134"), ("left", "1417"), ("header", "600"), ("footer", "600"), ("gutter", "0"));
        EmptyW(x, "cols", ("space", "708"));
        EmptyW(x, "docGrid", ("linePitch", "360"));
        x.WriteEndElement();
    }

    private static void WriteRunFonts(XmlWriter x)
        => EmptyW(x, "rFonts", ("ascii", "Times New Roman"), ("hAnsi", "Times New Roman"), ("eastAsia", "Times New Roman"), ("cs", "Times New Roman"));

    private static void EmptyW(XmlWriter x, string local, params (string Name, string Value)[] attributes)
    {
        x.WriteStartElement("w", local, WordNs);
        foreach (var (name, value) in attributes) x.WriteAttributeString("w", name, WordNs, value);
        x.WriteEndElement();
    }

    private static void Empty(XmlWriter x, string local, params (string Name, string Value)[] attributes)
    {
        x.WriteStartElement(local);
        foreach (var (name, value) in attributes) x.WriteAttributeString(name, value);
        x.WriteEndElement();
    }

    private static void Element(XmlWriter x, string local, string value)
    {
        x.WriteStartElement(local);
        x.WriteString(value);
        x.WriteEndElement();
    }

    private static void Element(XmlWriter x, string prefix, string local, string ns, string value)
    {
        x.WriteStartElement(prefix, local, ns);
        x.WriteString(value);
        x.WriteEndElement();
    }

    private static string ValueOr(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? "Chưa cập nhật";

    private static DateTime ToVietnamTime(DateTime value)
    {
        if (value.Kind == DateTimeKind.Unspecified) return value;
        var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        return utc.AddHours(7);
    }

    private static string PaymentStatus(TrangThaiThanhToanDon status) => status switch
    {
        TrangThaiThanhToanDon.DaThanhToan => "Đã thanh toán",
        TrangThaiThanhToanDon.ChoThanhToan => "Chờ thanh toán",
        TrangThaiThanhToanDon.ChoThuTienKhiNhanXe => "Chờ thu tiền khi nhận xe",
        TrangThaiThanhToanDon.DaHoanTien => "Đã hoàn tiền",
        TrangThaiThanhToanDon.ThatBai => "Thanh toán thất bại",
        _ => "Chưa lựa chọn"
    };

    private static string MoneyToWords(decimal amount)
    {
        var number = checked((long)Math.Round(amount, 0, MidpointRounding.AwayFromZero));
        if (number == 0) return "Không đồng";
        if (number < 0) return "Âm " + MoneyToWords(-number);

        var units = new[] { "", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ" };
        var groups = new List<int>();
        while (number > 0)
        {
            groups.Add((int)(number % 1000));
            number /= 1000;
        }

        var parts = new List<string>();
        for (var i = groups.Count - 1; i >= 0; i--)
        {
            if (groups[i] == 0) continue;
            var read = ReadThreeDigits(groups[i], full: i < groups.Count - 1 && groups[i] < 100);
            parts.Add(string.IsNullOrEmpty(units[i]) ? read : read + " " + units[i]);
        }
        var result = string.Join(" ", parts).Trim();
        return char.ToUpper(result[0], ViCulture) + result[1..] + " đồng";
    }

    private static string ReadThreeDigits(int number, bool full)
    {
        var digits = new[] { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
        var hundred = number / 100;
        var ten = number / 10 % 10;
        var unit = number % 10;
        var words = new List<string>();
        if (hundred > 0 || full)
        {
            words.Add(digits[hundred]);
            words.Add("trăm");
        }
        if (ten > 1)
        {
            words.Add(digits[ten]);
            words.Add("mươi");
            if (unit == 1) words.Add("mốt");
            else if (unit == 5) words.Add("lăm");
            else if (unit > 0) words.Add(digits[unit]);
        }
        else if (ten == 1)
        {
            words.Add("mười");
            if (unit == 5) words.Add("lăm");
            else if (unit > 0) words.Add(digits[unit]);
        }
        else if (unit > 0)
        {
            if (hundred > 0 || full) words.Add("lẻ");
            words.Add(unit == 5 ? "năm" : digits[unit]);
        }
        return string.Join(" ", words);
    }
}
