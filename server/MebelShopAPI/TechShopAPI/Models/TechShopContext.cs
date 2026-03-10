using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TechShopAPI.Models;

public partial class TechShopContext : DbContext
{
    public TechShopContext()
    {
    }

    public TechShopContext(DbContextOptions<TechShopContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Characteristic> Characteristics { get; set; }

    public virtual DbSet<EmailCode> EmailCodes { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCharacteristic> ProductCharacteristics { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<UserPayment> UserPayments { get; set; }
    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<VwOrdersWithItem> VwOrdersWithItems { get; set; }

    public virtual DbSet<VwProductsWithCategory> VwProductsWithCategories { get; set; }

    public virtual DbSet<VwUserFullName> VwUserFullNames { get; set; }
    public virtual DbSet<ViewOrder> ViewOrders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.IdAudit).HasName("PK__AuditLog__9225997E53AD0366");

            entity.ToTable("AuditLog");

            entity.Property(e => e.IdAudit).HasColumnName("ID_Audit");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__AuditLog__User_I__5441852A");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ProductId }).HasName("PK__CartItem__99EEDE2971CB0BED");

            entity.Property(e => e.UserId).HasColumnName("User_ID");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CartItems__Produ__73BA3083");

            entity.HasOne(d => d.User).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CartItems__User___72C60C4A");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.IdCategory).HasName("PK__Categori__6DB3A68A0C72A8C0");

            entity.Property(e => e.IdCategory).HasColumnName("ID_Category");
            entity.Property(e => e.NameCategory)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Characteristic>(entity =>
        {
            entity.HasKey(e => e.IdCharacteristic).HasName("PK__Characte__F86706C2C0158F7F");

            entity.Property(e => e.IdCharacteristic).HasColumnName("ID_Characteristic");
            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ValueType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("list");

            entity.HasOne(d => d.Category).WithMany(p => p.Characteristics)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Character__Categ__656C112C");
        });

        modelBuilder.Entity<EmailCode>(entity =>
        {
            entity.HasKey(e => e.IdCode).HasName("PK__EmailCod__560CEE3D51D02B9E");

            entity.Property(e => e.IdCode).HasColumnName("ID_Code");
            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ExpirationTime).HasColumnType("datetime");
            entity.Property(e => e.IsUsed).HasDefaultValue(false);
            entity.Property(e => e.Purpose)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.User).WithMany(p => p.EmailCodes)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__EmailCode__User___571DF1D5");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.IdOrder).HasName("PK__Orders__EC9FA955AE5948F2");

            entity.Property(e => e.IdOrder).HasColumnName("ID_Order");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Создан");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__User_ID__76969D2E");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.IdOrderItem).HasName("PK__OrderIte__0C9F19CB130E3A98");

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateStockOnOrder"));

            entity.Property(e => e.IdOrderItem).HasColumnName("ID_OrderItem");
            entity.Property(e => e.OrderId).HasColumnName("Order_ID");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__Order__7B5B524B");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__Produ__7C4F7684");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.IdProduct).HasName("PK__Products__522DE49672E0AB6D");

            entity.ToTable(tb => tb.HasTrigger("trg_AuditProducts"));

            entity.Property(e => e.IdProduct).HasColumnName("ID_Product");
            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");
            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductName)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Products__Catego__5DCAEF64");
        });

        modelBuilder.Entity<ProductCharacteristic>(entity =>
        {
            entity.HasKey(e => e.IdProductCharacteristic).HasName("PK__ProductC__AE3239F93A5B1FCA");

            entity.Property(e => e.IdProductCharacteristic).HasColumnName("ID_ProductCharacteristic");
            entity.Property(e => e.CharacteristicId).HasColumnName("Characteristic_ID");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.Value)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.Characteristic).WithMany(p => p.ProductCharacteristics)
                .HasForeignKey(d => d.CharacteristicId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProductCh__Chara__693CA210");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductCharacteristics)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProductCh__Produ__68487DD7");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.IdImage).HasName("PK__ProductI__31E45A2A331D5C94");

            entity.Property(e => e.IdImage).HasColumnName("ID_Image");
            entity.Property(e => e.ImageUrl)
                .IsUnicode(false)
                .HasColumnName("ImageURL");
            entity.Property(e => e.IsMain).HasDefaultValue(false);
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProductIm__Produ__60A75C0F");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.IdReview).HasName("PK__Reviews__E39E9647DF38E2B3");

            entity.Property(e => e.IdReview).HasColumnName("ID_Review");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.ReviewText).IsUnicode(false);
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__Product__6E01572D");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__User_ID__6EF57B66");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRole).HasName("PK__Roles__43DCD32D089351E3");

            entity.Property(e => e.IdRole).HasColumnName("ID_Role");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.IdUser).HasName("PK__Users__ED4DE442D3310FAF");

            entity.ToTable(tb => tb.HasTrigger("trg_AuditUsers"));

            entity.HasIndex(e => e.Email, "UQ__Users__A9D1053448E48004").IsUnique();

            entity.Property(e => e.IdUser).HasColumnName("ID_User");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash).IsUnicode(false);
            entity.Property(e => e.RoleId).HasColumnName("Role_ID");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__Role_ID__4CA06362");
        });

        modelBuilder.Entity<UserPayment>()
            .HasKey(up => up.UserId);

        modelBuilder.Entity<UserPayment>()
            .Property(up => up.UserId)
            .HasColumnName("User_ID");

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__UserProf__206D9190E6CD7D5C");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("User_ID");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Surname)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Theme)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Light");

            entity.HasOne(d => d.User).WithOne(p => p.UserProfile)
                .HasForeignKey<UserProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserProfi__User___5070F446");
        });

        modelBuilder.Entity<VwOrdersWithItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_OrdersWithItems");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.IdOrder).HasColumnName("ID_Order");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.ProductName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(29, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UserId).HasColumnName("User_ID");
        });

        modelBuilder.Entity<VwProductsWithCategory>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ProductsWithCategory");

            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.IdProduct).HasColumnName("ID_Product");
            entity.Property(e => e.NameCategory)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductName)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwUserFullName>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_UserFullNames");

            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FullName)
                .HasMaxLength(101)
                .IsUnicode(false);
            entity.Property(e => e.IdUser).HasColumnName("ID_User");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ViewOrder>()
            .HasNoKey()
            .ToView("View_Orders");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
