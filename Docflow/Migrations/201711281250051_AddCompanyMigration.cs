namespace Docflow.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCompanyMigration : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DocumentModel",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        Name = c.String(),
                        Content = c.Binary(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.DocumentModel");
        }
    }
}
