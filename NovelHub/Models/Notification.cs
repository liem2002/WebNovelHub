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
    
    public partial class Notification
    {
        public int NotificationID { get; set; }
        public Nullable<int> UserID { get; set; }
        public string Content { get; set; }
        public Nullable<System.DateTime> Timestamp { get; set; }
        public Nullable<bool> ReadStatus { get; set; }
    
        public virtual User User { get; set; }
    }
}
