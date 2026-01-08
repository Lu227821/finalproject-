namespace WebApplication1.Models
{
    public class Record
    {
        public int Id { get; set; }
        public string? 出表日期 { get; set; }
        public string? 資料年月 { get; set; }
        public string? 公司代號 { get; set; }
        public string? 公司名稱 { get; set; }
        public string? 產業別 { get; set; }
        public string? 營業收入_當月營收 { get; set; }
        public string? 營業收入_上月營收 { get; set; }
        public string? 營業收入_去年當月營收 { get; set; }
        public string? 營業收入_上月比較增減_pct { get; set; }
        public string? 營業收入_去年同月增減_pct { get; set; }
        public string? 累計營業收入_當月累計營收 { get; set; }
        public string? 累計營業收入_去年累計營收 { get; set; }
        public string? 累計營業收入_前期比較增減_pct { get; set; }
        public string? 備註 { get; set; }
    }
}
