using System.ComponentModel.DataAnnotations.Schema;

namespace PoE_Price_Tracking
{
    [Table("prices")]
    public class PriceRecord
    {
        [Column("id")]
        public int Id { get; set; }
    
        [Column("item_id")]
        public int ItemId { get; set; }
    
        [Column("chaos_equal")]
        public double? ChaosEqual { get; set; }
    
        [Column("prev_chaos_equal")]
        public double? PrevChaosEqual { get; set; }

        [Column("price")]
        public double Price { get; set; }

        [Column("currency")]
        public string Currency { get; set; } = "";

        [Column("league")]
        public string League { get; set; } = "Standard";
    }
}