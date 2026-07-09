using System.Globalization;
using System.Text;

namespace Ordevo.Modules.Print.Application;

public static class EscPosEncoder
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly byte[] Init = [0x1B, 0x40];
    private static readonly byte[] CodePage857 = [0x1B, 0x74, 0x0D];
    private static readonly byte[] AlignLeft = [0x1B, 0x61, 0x00];
    private static readonly byte[] AlignCenter = [0x1B, 0x61, 0x01];
    private static readonly byte[] BoldOn = [0x1B, 0x45, 0x01];
    private static readonly byte[] BoldOff = [0x1B, 0x45, 0x00];
    private static readonly byte[] DoubleOn = [0x1D, 0x21, 0x11];
    private static readonly byte[] DoubleOff = [0x1D, 0x21, 0x00];
    private static readonly byte[] FeedCut = [0x0A, 0x0A, 0x0A, 0x1D, 0x56, 0x42, 0x00];

    public static byte[] Receipt(ReceiptDocumentDto d, string businessName, int width = 42)
    {
        var w = new EscWriter(width);
        w.Raw(Init).Raw(CodePage857);

        w.Raw(AlignCenter).Raw(BoldOn).Raw(DoubleOn).Line(businessName).Raw(DoubleOff).Raw(BoldOff);
        w.Line(d.TableName is null ? d.OrderType.ToUpper(Tr) : d.TableName);
        w.Line($"Adisyon #{d.OrderNo}");
        if (d.InvoiceNo is not null) w.Line($"Fatura No: {d.InvoiceNo}");
        w.Line((d.ClosedAt ?? d.OpenedAt).ToLocalTime().ToString("dd.MM.yyyy HH:mm", Tr));
        w.Raw(AlignLeft).Rule();

        foreach (var l in d.Lines)
        {
            w.Line($"{Qty(l.Quantity)} x {l.Name}");
            w.TwoCol("", Money(l.LineTotal));
            if (!string.IsNullOrWhiteSpace(l.Note)) w.Line($"  * {l.Note}");
        }

        w.Rule();
        w.TwoCol("Ara Toplam", Money(d.Subtotal));
        if (d.DiscountTotal > 0) w.TwoCol("Indirim", "-" + Money(d.DiscountTotal));
        w.TwoCol("KDV", Money(d.TaxTotal));
        w.Raw(BoldOn).Raw(DoubleOn).TwoCol("TOPLAM", Money(d.Total)).Raw(DoubleOff).Raw(BoldOff);

        if (d.Payments.Count > 0)
        {
            w.Rule();
            foreach (var p in d.Payments)
                w.TwoCol(PaymentLabel(p.Method) + (p.TipAmount > 0 ? $" (+{Money(p.TipAmount)} bahsis)" : ""), Money(p.Amount));
        }

        w.Rule();
        w.Raw(AlignCenter).Line("Bizi tercih ettiginiz icin tesekkurler!").Line("Afiyet olsun.");
        w.Raw(FeedCut);
        return w.ToArray();
    }

    public static byte[] KitchenTicket(KitchenTicketDocumentDto d, int width = 42)
    {
        var w = new EscWriter(width);
        w.Raw(Init).Raw(CodePage857);

        w.Raw(AlignCenter).Raw(BoldOn).Raw(DoubleOn).Line("MUTFAK").Raw(DoubleOff);
        w.Line(d.TableName ?? d.OrderType.ToUpper(Tr));
        w.Line($"Adisyon #{d.OrderNo}").Raw(BoldOff);
        w.Line(d.OpenedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm", Tr));
        w.Raw(AlignLeft).Rule();

        foreach (var l in d.Lines)
        {
            w.Raw(BoldOn).Raw(DoubleOn).Line($"{Qty(l.Quantity)} x {l.Name}").Raw(DoubleOff).Raw(BoldOff);
            if (!string.IsNullOrWhiteSpace(l.Modifiers)) w.Line($"  {l.Modifiers}");
            if (!string.IsNullOrWhiteSpace(l.Note)) w.Line($"  NOT: {l.Note}");
            if (!string.IsNullOrWhiteSpace(l.Station)) w.Line($"  [{l.Station}]");
        }

        w.Raw(FeedCut);
        return w.ToArray();
    }

    private static string Money(decimal v) => v.ToString("N2", Tr) + " TL";
    private static string Qty(decimal q) => q == Math.Truncate(q) ? ((long)q).ToString() : q.ToString("0.###", Tr);

    private static string PaymentLabel(string method) => method switch
    {
        "cash" => "Nakit",
        "card" => "Kredi Karti",
        "meal_voucher" => "Yemek Karti",
        "on_account" => "Cari Hesap",
        _ => method
    };

    private sealed class EscWriter(int width)
    {
        private readonly List<byte> _bytes = [];

        public EscWriter Raw(byte[] cmd) { _bytes.AddRange(cmd); return this; }

        public EscWriter Line(string text) { _bytes.AddRange(Cp857(text)); _bytes.Add(0x0A); return this; }

        public EscWriter Rule() => Line(new string('-', width));

        public EscWriter TwoCol(string left, string right)
        {
            var space = width - left.Length - right.Length;
            if (space < 1) space = 1;
            return Line(left + new string(' ', space) + right);
        }

        public byte[] ToArray() => _bytes.ToArray();

        private static byte[] Cp857(string text)
        {
            var buf = new byte[text.Length];
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                buf[i] = c switch
                {
                    'ç' => 0x87, 'Ç' => 0x80, 'ğ' => 0xA7, 'Ğ' => 0xA6,
                    'ı' => 0x8D, 'İ' => 0x98, 'ş' => 0x9F, 'Ş' => 0x9E,
                    'ö' => 0x94, 'Ö' => 0x99, 'ü' => 0x81, 'Ü' => 0x9A,
                    <= (char)0x7F => (byte)c,
                    _ => (byte)'?'
                };
            }
            return buf;
        }
    }
}
