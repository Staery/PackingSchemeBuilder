namespace EntityFrameworkSqliteCodeFirst.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class initBottle : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Bottle",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(maxLength: 2147483647),
                        BoxId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Boxes", t => t.BoxId, cascadeDelete: true)
                .Index(t => t.BoxId, name: "'IX_Bottle_BoxId'");
            
            CreateTable(
                "dbo.Boxes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(maxLength: 2147483647),
                        PalletId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Pallets", t => t.PalletId, cascadeDelete: true)
                .Index(t => t.PalletId, name: "'IX_Box_PalletId'");
            
            CreateTable(
                "dbo.Pallets",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(maxLength: 2147483647),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Bottle", "BoxId", "dbo.Boxes");
            DropForeignKey("dbo.Boxes", "PalletId", "dbo.Pallets");
            DropIndex("dbo.Boxes", "'IX_Box_PalletId'");
            DropIndex("dbo.Bottle", "'IX_Bottle_BoxId'");
            DropTable("dbo.Pallets");
            DropTable("dbo.Boxes");
            DropTable("dbo.Bottle");
        }
    }
}
