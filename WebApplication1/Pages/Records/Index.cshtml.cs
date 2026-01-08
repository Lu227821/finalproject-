using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using WebApplication1.Models;
using System.Linq;

namespace WebApplication1.Pages.Records
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;

        public IndexModel(IConfiguration config)
        {
            _config = config;
        }

        public List<Record> Items { get; set; } = new List<Record>();
        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public string? 公司代號 { get; set; }

        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public string? 公司名稱 { get; set; }

        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public string? 產業別 { get; set; }

        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public string? 資料年月 { get; set; }

        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public int TopN { get; set; }
        public int TotalCount { get; set; }
        public int DistinctCompaniesCount { get; set; }
        public List<DuplicateGroup> DuplicateGroups { get; set; } = new List<DuplicateGroup>();

        public void OnGet()
        {
            var connStr = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connStr))
            {
                return;
            }

            using var conn = new SqlConnection(connStr);
            // Build parameterized query based on filters
            var sql = "SELECT TOP (1000) [Id],[出表日期],[資料年月],[公司代號],[公司名稱],[產業別],[營業收入_當月營收],[營業收入_上月營收],[營業收入_去年當月營收],[營業收入_上月比較增減_pct],[營業收入_去年同月增減_pct],[累計營業收入_當月累計營收],[累計營業收入_去年累計營收],[累計營業收入_前期比較增減_pct],[備註] FROM dbo.Records";
            var where = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(公司代號))
            {
                where.Add("[公司代號] = @公司代號");
                parameters.Add(new SqlParameter("@公司代號", 公司代號));
            }
            if (!string.IsNullOrWhiteSpace(公司名稱))
            {
                where.Add("[公司名稱] LIKE @公司名稱");
                parameters.Add(new SqlParameter("@公司名稱", "%" + 公司名稱 + "%"));
            }
            if (!string.IsNullOrWhiteSpace(產業別))
            {
                where.Add("[產業別] LIKE @產業別");
                parameters.Add(new SqlParameter("@產業別", "%" + 產業別 + "%"));
            }
            if (!string.IsNullOrWhiteSpace(資料年月))
            {
                where.Add("[資料年月] = @資料年月");
                parameters.Add(new SqlParameter("@資料年月", 資料年月));
            }

            if (where.Count > 0)
            {
                sql += " WHERE " + string.Join(" AND ", where);
            }

            sql += " ORDER BY Id";

            using var cmd = new SqlCommand(sql, conn);
            if (parameters.Count > 0)
            {
                cmd.Parameters.AddRange(parameters.ToArray());
            }

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var r = new Record
                {
                    Id = reader.GetInt32(0),
                    出表日期 = reader.IsDBNull(1) ? null : reader.GetString(1),
                    資料年月 = reader.IsDBNull(2) ? null : reader.GetString(2),
                    公司代號 = reader.IsDBNull(3) ? null : reader.GetString(3),
                    公司名稱 = reader.IsDBNull(4) ? null : reader.GetString(4),
                    產業別 = reader.IsDBNull(5) ? null : reader.GetString(5),
                    營業收入_當月營收 = reader.IsDBNull(6) ? null : reader.GetString(6),
                    營業收入_上月營收 = reader.IsDBNull(7) ? null : reader.GetString(7),
                    營業收入_去年當月營收 = reader.IsDBNull(8) ? null : reader.GetString(8),
                    營業收入_上月比較增減_pct = reader.IsDBNull(9) ? null : reader.GetValue(9).ToString(),
                    營業收入_去年同月增減_pct = reader.IsDBNull(10) ? null : reader.GetValue(10).ToString(),
                    累計營業收入_當月累計營收 = reader.IsDBNull(11) ? null : reader.GetValue(11).ToString(),
                    累計營業收入_去年累計營收 = reader.IsDBNull(12) ? null : reader.GetValue(12).ToString(),
                    累計營業收入_前期比較增減_pct = reader.IsDBNull(13) ? null : reader.GetValue(13).ToString(),
                    備註 = reader.IsDBNull(14) ? null : reader.GetString(14),
                };
                Items.Add(r);
            }

            // If TopN specified, select top N by 當月營收
            if (TopN > 0)
            {
                var pairs = Items.Select(r => (rec: r, val: ParseDecimal(r.營業收入_當月營收))).OrderByDescending(x => x.val).Take(TopN).Select(x => x.rec).ToList();
                Items = pairs;
            }

            // compute counts and detect duplicates by 公司代號 or 公司名稱
            TotalCount = Items.Count;
            DistinctCompaniesCount = Items.Select(r => (r.公司代號 ?? r.公司名稱 ?? string.Empty).Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct().Count();

            DuplicateGroups = Items
                .GroupBy(r => (r.公司代號 ?? r.公司名稱 ?? string.Empty).Trim())
                .Where(g => g.Count() > 1)
                .Select(g => new DuplicateGroup { Key = g.Key, Count = g.Count(), Ids = g.Select(x => x.Id).ToList() })
                .ToList();
        }

        private decimal ParseDecimal(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0m;
            var cleaned = new string((s ?? string.Empty).Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
            if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
            if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out d)) return d;
            return 0m;
        }
    }

public class DuplicateGroup
{
    public string Key { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<int> Ids { get; set; } = new List<int>();
}
}
