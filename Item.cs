using System.ComponentModel.DataAnnotations.Schema;

namespace PoE_Price_Tracking
{
    [Table("uniq_items")]
    public class Item
    {
        [Column("id")]
        public int Id {get;set; }
        [Column("name")]
        public string Name { get; set; } = "";  
        [Column("item_type")]
        public string ItemType { get; set; } = "";    
        [Column("item_sub_type")]
        public string ItemSubType { get; set; } = "";      
        [NotMapped]
        public string Icon { get; set; } = ""; 
    }
}