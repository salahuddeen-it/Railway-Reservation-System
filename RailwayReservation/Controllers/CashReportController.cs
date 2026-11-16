using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.Models;

namespace RailwayReservation.Controllers
{
    /// <summary>Daily cash transactions: money received / refunded, export to Excel (CSV).</summary>
    [Authorize(Roles = AppRoles.AdminOrClerk)]
    public class CashReportController : BaseController
    {
        // GET: /CashReport?date=2026-11-16
        public ActionResult Index(DateTime? date)
        {
            DateTime d = (date ?? DateTime.Today).Date;
            ViewBag.Date = d;
            return View(GetTransactions(d));
        }

        // GET: /CashReport/Export?date=2026-11-16 → opens in MS Excel
        public ActionResult Export(DateTime? date)
        {
            DateTime d = (date ?? DateTime.Today).Date;
            var list = GetTransactions(d);

            decimal received = list.Where(t => t.Type == CashTransactionType.Receipt).Sum(t => t.Amount);
            decimal refunded = list.Where(t => t.Type == CashTransactionType.Refund).Sum(t => t.Amount);

            var sb = new StringBuilder();
            sb.AppendLine("Daily Cash Transactions," + Csv(d.ToString("dd-MMM-yyyy")));
            sb.AppendLine();
            sb.AppendLine("Date/Time,PNR,Type,Amount,User,Remarks");
            foreach (var t in list)
            {
                sb.AppendLine(string.Join(",",
                    Csv(t.TransactionDate.ToString("dd-MM-yyyy HH:mm")),
                    Csv(t.Pnr),
                    Csv(t.Type.ToString()),
                    t.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                    Csv(t.User != null ? t.User.UserName : ""),
                    Csv(t.Remarks)));
            }
            sb.AppendLine();
            sb.AppendLine("Total Received,," + "," + received.ToString("0.00", CultureInfo.InvariantCulture));
            sb.AppendLine("Total Refunded,," + "," + refunded.ToString("0.00", CultureInfo.InvariantCulture));
            sb.AppendLine("Net Collection,," + "," + (received - refunded).ToString("0.00", CultureInfo.InvariantCulture));

            // UTF-8 with BOM so Excel shows text correctly
            byte[] body = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(body, "text/csv", "DailyCash_" + d.ToString("yyyyMMdd") + ".csv");
        }

        private List<CashTransaction> GetTransactions(DateTime d)
        {
            DateTime next = d.AddDays(1);
            return Db.CashTransactions.Include(c => c.User)
                .Where(c => c.TransactionDate >= d && c.TransactionDate < next)
                .OrderBy(c => c.TransactionDate)
                .ToList();
        }

        // Quote a CSV value; block Excel formula injection (=, +, -, @)
        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            if ("=+-@".IndexOf(value[0]) >= 0) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}