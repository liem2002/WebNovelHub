//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace NovelHub.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class BlacklistedNovel
    {
        public int BlacklistedNovelID { get; set; }
        public Nullable<int> NovelID { get; set; }
        public Nullable<System.DateTime> CreatedAt { get; set; }
        public string Note { get; set; }
    
        public virtual Novel Novel { get; set; }
    }
}