using System;
using System.Collections.Generic;

namespace CatatanKeuanganDotnet.Dtos.Transactions
{
    /// <summary>
    /// Representasi response utama untuk endpoint rekap transaksi.
    /// Mengikuti struktur JSON yang disepakati dengan tim frontend.
    /// </summary>
    public class TransactionRecapResponse
    {
        /// <summary>
        /// Status string sederhana agar frontend tidak perlu menerjemahkan nilai boolean.
        /// </summary>
        public string Status { get; set; } = "success";

        /// <summary>
        /// Menandai mode periode aktif (day/week/month) agar frontend bisa memilih renderer yang tepat.
        /// </summary>
        public string Period { get; set; } = string.Empty;

        /// <summary>
        /// Tanggal mulai rekap dalam rentang bulan berjalan.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Tanggal akhir rekap, biasanya tanggal hari ini.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Koleksi data rekap yang sudah diklaster per hari/minggu/bulan.
        /// </summary>
        public IReadOnlyCollection<TransactionRecapItem> Data { get; set; } = Array.Empty<TransactionRecapItem>();
    }

    /// <summary>
    /// Detail rekap untuk satu cluster periode (hari, minggu, atau bulan).
    /// </summary>
    public class TransactionRecapItem
    {
        /// <summary>
        /// Label yang akan langsung dipakai frontend (ISO date atau range human readable).
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Tanggal mulai periode cluster agar frontend bisa menampilkan rentang waktu.
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// Tanggal akhir periode cluster.
        /// </summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        /// Daftar transaksi mentah agar frontend bisa menampilkan detail modal atau tabel.
        /// </summary>
        public IReadOnlyCollection<TransactionResponse> Transactions { get; set; } = Array.Empty<TransactionResponse>();

        /// <summary>
        /// Nominal total pada periode cluster ini (income positif, expense negatif sesuai input).
        /// </summary>
        public decimal TotalAmount { get; set; }
    }
}
