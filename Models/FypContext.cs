using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FYPBidNetra.Models;

public partial class FypContext : DbContext
{
    public FypContext()
    {
    }

    public FypContext(DbContextOptions<FypContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuctionBid> AuctionBids { get; set; }

    public virtual DbSet<AuctionDetail> AuctionDetails { get; set; }

    public virtual DbSet<Bank> Banks { get; set; }

    public virtual DbSet<Bid> Bids { get; set; }

    public virtual DbSet<BlogContent> BlogContents { get; set; }

    public virtual DbSet<Chat> Chats { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<ContractDetail> ContractDetails { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Rating> Ratings { get; set; }

    public virtual DbSet<TenderApplication> TenderApplications { get; set; }

    public virtual DbSet<TenderDetail> TenderDetails { get; set; }

    public virtual DbSet<Term> Terms { get; set; }

    public virtual DbSet<UserList> UserLists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("name=dbConn");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuctionBid>(entity =>
        {
            entity.HasKey(e => e.BidId).HasName("PK__AuctionB__4A733D923753E8AC");

            entity.ToTable("AuctionBid");

            entity.Property(e => e.BidId).ValueGeneratedNever();
            entity.Property(e => e.BidAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BidStatus).HasMaxLength(20);

            entity.HasOne(d => d.AuctionBidNavigation).WithMany(p => p.AuctionBids)
                .HasForeignKey(d => d.AuctionBidId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AuctionBi__Aucti__5CD6CB2B");

            entity.HasOne(d => d.Bidder).WithMany(p => p.AuctionBids)
                .HasForeignKey(d => d.BidderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AuctionBi__Bidde__5DCAEF64");
        });

        modelBuilder.Entity<AuctionDetail>(entity =>
        {
            entity.HasKey(e => e.AuctionId).HasName("PK__AuctionD__51004A4C300F03F9");

            entity.Property(e => e.AuctionId).ValueGeneratedNever();
            entity.Property(e => e.AuctionStatus).HasMaxLength(20);
            entity.Property(e => e.AuctionType).HasMaxLength(50);
            entity.Property(e => e.AwardStatus).HasMaxLength(20);
            entity.Property(e => e.BuyerId).HasColumnName("BuyerID");
            entity.Property(e => e.IsVerified).HasMaxLength(20);
            entity.Property(e => e.StartingPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.WinningBidAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Buyer).WithMany(p => p.AuctionDetailBuyers)
                .HasForeignKey(d => d.BuyerId)
                .HasConstraintName("FK__AuctionDe__Buyer__571DF1D5");

            entity.HasOne(d => d.PublishedByUser).WithMany(p => p.AuctionDetailPublishedByUsers)
                .HasForeignKey(d => d.PublishedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AuctionDe__Publi__5812160E");
        });

        modelBuilder.Entity<Bank>(entity =>
        {
            entity.HasKey(e => e.BankId).HasName("PK__Bank__AA08CB1384CE4B25");

            entity.ToTable("Bank");

            entity.Property(e => e.BankId).ValueGeneratedNever();
            entity.Property(e => e.AccountHolderName).HasMaxLength(100);
            entity.Property(e => e.AccountNumber).HasMaxLength(100);
            entity.Property(e => e.AccountType).HasMaxLength(50);
            entity.Property(e => e.BankName).HasMaxLength(100);

            entity.HasOne(d => d.Userbank).WithMany(p => p.Banks)
                .HasForeignKey(d => d.UserbankId)
                .HasConstraintName("FK__Bank__UserbankId__46E78A0C");
        });

        modelBuilder.Entity<Bid>(entity =>
        {
            entity.HasKey(e => e.BiddingId).HasName("PK__Bid__B1AE7D4789F35230");

            entity.ToTable("Bid");

            entity.Property(e => e.BiddingId).ValueGeneratedNever();
            entity.Property(e => e.BiddingAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.AucBid).WithMany(p => p.Bids)
                .HasForeignKey(d => d.AucBidId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Bid__AucBidId__619B8048");
        });

        modelBuilder.Entity<BlogContent>(entity =>
        {
            entity.HasKey(e => e.Bid).HasName("PK__BlogCont__C6DE0CC10B06795F");

            entity.ToTable("BlogContent");

            entity.Property(e => e.Bid)
                .ValueGeneratedNever()
                .HasColumnName("BId");

            entity.HasOne(d => d.UploadUser).WithMany(p => p.BlogContents)
                .HasForeignKey(d => d.UploadUserId)
                .HasConstraintName("FK__BlogConte__Uploa__6477ECF3");
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.ChatId).HasName("PK__Chat__A9FBE7C61D2FB1CF");

            entity.ToTable("Chat");

            entity.Property(e => e.ChatId).ValueGeneratedNever();
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Receiver).WithMany(p => p.ChatReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Chat__ReceiverId__68487DD7");

            entity.HasOne(d => d.Sender).WithMany(p => p.ChatSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Chat__SenderId__6754599E");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.CompanyId).HasName("PK__Company__2D971CACA5D04071");

            entity.ToTable("Company");

            entity.HasIndex(e => e.PanNumber, "UQ__Company__7C38BFC873608A91").IsUnique();

            entity.HasIndex(e => e.RegistrationNumber, "UQ__Company__E8864602125AF5CE").IsUnique();

            entity.HasIndex(e => e.OfficeEmail, "UQ__Company__FCEC3C7289C86E63").IsUnique();

            entity.Property(e => e.CompanyId).ValueGeneratedNever();
            entity.Property(e => e.CompanyName).HasMaxLength(100);
            entity.Property(e => e.CompanyType).HasMaxLength(50);
            entity.Property(e => e.CompanyWebsiteUrl).HasMaxLength(255);
            entity.Property(e => e.FullAddress).HasMaxLength(50);
            entity.Property(e => e.OfficeEmail).HasMaxLength(100);
            entity.Property(e => e.PanNumber).HasMaxLength(20);
            entity.Property(e => e.Position).HasMaxLength(50);
            entity.Property(e => e.Rating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.RegistrationNumber).HasMaxLength(50);

            entity.HasOne(d => d.Userbid).WithMany(p => p.Companies)
                .HasForeignKey(d => d.UserbidId)
                .HasConstraintName("FK__Company__Userbid__3F466844");
        });

        modelBuilder.Entity<ContractDetail>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__Contract__C90D3469ED83092C");

            entity.Property(e => e.ContractId).ValueGeneratedNever();
            entity.Property(e => e.BuyerId).HasColumnName("BuyerID");
            entity.Property(e => e.ContractStatus).HasMaxLength(20);
            entity.Property(e => e.SellerId).HasColumnName("SellerID");

            entity.HasOne(d => d.Buyer).WithMany(p => p.ContractDetailBuyers)
                .HasForeignKey(d => d.BuyerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ContractD__Buyer__6FE99F9F");

            entity.HasOne(d => d.ConAuction).WithMany(p => p.ContractDetails)
                .HasForeignKey(d => d.ConAuctionId)
                .HasConstraintName("FK__ContractD__ConAu__6E01572D");

            entity.HasOne(d => d.ConCompany).WithMany(p => p.ContractDetails)
                .HasForeignKey(d => d.ConCompanyId)
                .HasConstraintName("FK__ContractD__ConCo__6D0D32F4");

            entity.HasOne(d => d.ConTender).WithMany(p => p.ContractDetails)
                .HasForeignKey(d => d.ConTenderId)
                .HasConstraintName("FK__ContractD__ConTe__6C190EBB");

            entity.HasOne(d => d.Seller).WithMany(p => p.ContractDetailSellers)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ContractD__Selle__6EF57B66");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__9B556A38C6C820DE");

            entity.ToTable("Payment");

            entity.Property(e => e.PaymentId).ValueGeneratedNever();
            entity.Property(e => e.PaymentAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasMaxLength(20);

            entity.HasOne(d => d.PayAuction).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PayAuctionId)
                .HasConstraintName("FK__Payment__PayAuct__7C4F7684");

            entity.HasOne(d => d.PayByUserNavigation).WithMany(p => p.PaymentPayByUserNavigations)
                .HasForeignKey(d => d.PayByUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__PayByUs__7E37BEF6");

            entity.HasOne(d => d.PayCompany).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PayCompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__PayComp__7B5B524B");

            entity.HasOne(d => d.PayTender).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PayTenderId)
                .HasConstraintName("FK__Payment__PayTend__7A672E12");

            entity.HasOne(d => d.PayToUserNavigation).WithMany(p => p.PaymentPayToUserNavigations)
                .HasForeignKey(d => d.PayToUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__PayToUs__7D439ABD");
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => e.RatingId).HasName("PK__Rating__FCCDF87C937449F8");

            entity.ToTable("Rating");

            entity.Property(e => e.RatingId).ValueGeneratedNever();
            entity.Property(e => e.Rating1)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("Rating");
            entity.Property(e => e.RatingDescription).HasMaxLength(500);

            entity.HasOne(d => d.RatingByNavigation).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.RatingBy)
                .HasConstraintName("FK__Rating__RatingBy__4316F928");

            entity.HasOne(d => d.RatingForNavigation).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.RatingFor)
                .HasConstraintName("FK__Rating__RatingFo__440B1D61");
        });

        modelBuilder.Entity<TenderApplication>(entity =>
        {
            entity.HasKey(e => e.ApplicationId).HasName("PK__TenderAp__C93A4C99DF51B3A3");

            entity.ToTable("TenderApplication");

            entity.Property(e => e.ApplicationId).ValueGeneratedNever();
            entity.Property(e => e.ApplicationStatus).HasMaxLength(10);
            entity.Property(e => e.ProposedBudget).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProposedDuration).HasMaxLength(50);

            entity.HasOne(d => d.CompanyApply).WithMany(p => p.TenderApplications)
                .HasForeignKey(d => d.CompanyApplyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TenderApp__Compa__5165187F");

            entity.HasOne(d => d.TenderApplly).WithMany(p => p.TenderApplications)
                .HasForeignKey(d => d.TenderAppllyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TenderApp__Tende__5070F446");
        });

        modelBuilder.Entity<TenderDetail>(entity =>
        {
            entity.HasKey(e => e.TenderId).HasName("PK__TenderDe__B21B4268B86E3C20");

            entity.Property(e => e.TenderId).ValueGeneratedNever();
            entity.Property(e => e.AwardStatus).HasMaxLength(20);
            entity.Property(e => e.BudgetEstimation).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IsVerified).HasMaxLength(20);
            entity.Property(e => e.IssuedBy).HasMaxLength(100);
            entity.Property(e => e.ProjectDuration).HasMaxLength(50);
            entity.Property(e => e.TenderStatus).HasMaxLength(20);
            entity.Property(e => e.TenderType).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(100);

            entity.HasOne(d => d.AwardCompany).WithMany(p => p.TenderDetails)
                .HasForeignKey(d => d.AwardCompanyId)
                .HasConstraintName("FK__TenderDet__Award__49C3F6B7");

            entity.HasOne(d => d.PublishedByUser).WithMany(p => p.TenderDetails)
                .HasForeignKey(d => d.PublishedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TenderDet__Publi__4AB81AF0");
        });

        modelBuilder.Entity<Term>(entity =>
        {
            entity.HasKey(e => e.TermId).HasName("PK__Terms__410A21A54F7406C3");

            entity.Property(e => e.TermId).ValueGeneratedNever();
            entity.Property(e => e.ContractId).HasColumnName("ContractID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Contract).WithMany(p => p.Terms)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Terms__ContractI__75A278F5");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Terms)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Terms__CreatedBy__778AC167");
        });

        modelBuilder.Entity<UserList>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__UserList__1788CC4CC254FB20");

            entity.ToTable("UserList");

            entity.HasIndex(e => e.EmailAddress, "UQ__UserList__49A14740ACE3FBDA").IsUnique();

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.District).HasMaxLength(50);
            entity.Property(e => e.EmailAddress).HasMaxLength(50);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.Gender).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.MiddleName).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Province).HasMaxLength(50);
            entity.Property(e => e.UserRole).HasMaxLength(40);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
