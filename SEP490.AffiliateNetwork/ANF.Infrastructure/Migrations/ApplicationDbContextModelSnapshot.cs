﻿// <auto-generated />
using System;
using ANF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ANF.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ANF.Core.Models.Entities.AdvertiserProfile", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("adv_no");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long>("AdvertiserId")
                        .HasColumnType("bigint")
                        .HasColumnName("advertiser_id");

                    b.Property<string>("Bio")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("bio");

                    b.Property<string>("CompanyName")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("company_name");

                    b.Property<string>("ImageUrl")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("image_url");

                    b.Property<string>("Industry")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("industry");

                    b.HasKey("Id");

                    b.HasIndex("AdvertiserId")
                        .IsUnique();

                    b.ToTable("AdvertiserProfiles");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Campaign", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("camp_id");

                    b.Property<Guid>("AdvertiserCode")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("advertiser_code");

                    b.Property<double?>("Balance")
                        .HasColumnType("float")
                        .HasColumnName("balance");

                    b.Property<long?>("CategoryId")
                        .HasColumnType("bigint")
                        .HasColumnName("cate_id");

                    b.Property<byte[]>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .IsRequired()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion")
                        .HasColumnName("concurrency_stamp");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("description");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("datetime2")
                        .HasColumnName("end_date");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("camp_name");

                    b.Property<string>("ProductUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("product_url");

                    b.Property<string>("RejectReason")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("reject_reason");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("datetime2")
                        .HasColumnName("start_date");

                    b.Property<int>("Status")
                        .HasColumnType("int")
                        .HasColumnName("camp_status");

                    b.Property<string>("TrackingParams")
                        .HasColumnType("text")
                        .HasColumnName("tracking_params");

                    b.HasKey("Id");

                    b.HasIndex("AdvertiserCode");

                    b.HasIndex("CategoryId");

                    b.ToTable("Campaigns");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.CampaignImage", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("img_no");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("AddedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("added_at");

                    b.Property<long?>("CampaignId")
                        .HasColumnType("bigint")
                        .HasColumnName("camp_id");

                    b.Property<string>("ImageUrl")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("img_url");

                    b.HasKey("Id");

                    b.HasIndex("CampaignId");

                    b.ToTable("CampaignImages");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Category", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("cate_id");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("description");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("cate_name");

                    b.HasKey("Id");

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Offer", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("offer_id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<double>("Bid")
                        .HasColumnType("float")
                        .HasColumnName("bid");

                    b.Property<double>("Budget")
                        .HasColumnType("float")
                        .HasColumnName("budget");

                    b.Property<long>("CampaignId")
                        .HasColumnType("bigint")
                        .HasColumnName("camp_id");

                    b.Property<double?>("CommissionRate")
                        .HasColumnType("float")
                        .HasColumnName("commission_rate");

                    b.Property<byte[]>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .IsRequired()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion")
                        .HasColumnName("concurrency_stamp");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("offer_description");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("datetime2")
                        .HasColumnName("end_date");

                    b.Property<string>("ImageUrl")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("img_url");

                    b.Property<string>("OrderReturnTime")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("order_return_time");

                    b.Property<string>("PricingModel")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("pricing_model");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("datetime2")
                        .HasColumnName("start_date");

                    b.Property<string>("StepInfo")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("step_info");

                    b.HasKey("Id");

                    b.HasIndex("CampaignId");

                    b.ToTable("Offers");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.PostbackData", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("pbd_no");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<double?>("Amount")
                        .HasColumnType("float")
                        .HasColumnName("amount");

                    b.Property<Guid>("ClickId")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("click_id");

                    b.Property<long>("OfferId")
                        .HasColumnType("bigint")
                        .HasColumnName("offer_id");

                    b.Property<long>("PublisherId")
                        .HasColumnType("bigint")
                        .HasColumnName("publisher_id");

                    b.Property<int>("Status")
                        .HasColumnType("int")
                        .HasColumnName("status");

                    b.HasKey("Id");

                    b.HasIndex("OfferId");

                    b.ToTable("PostbackData");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.PublisherOffer", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("po_no");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("JoiningDate")
                        .HasColumnType("datetime2")
                        .HasColumnName("joining_date");

                    b.Property<long>("OfferId")
                        .HasColumnType("bigint")
                        .HasColumnName("offer_id");

                    b.Property<Guid>("PublisherCode")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("publisher_code");

                    b.Property<string>("RejectReason")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("reject_reason");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("OfferId");

                    b.HasIndex("PublisherCode");

                    b.ToTable("PublisherOffers");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.PublisherProfile", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("pub_no");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Bio")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("bio");

                    b.Property<string>("ImageUrl")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("image_url");

                    b.Property<long>("PublisherId")
                        .HasColumnType("bigint")
                        .HasColumnName("publisher_id");

                    b.Property<string>("Specialization")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("specialization");

                    b.HasKey("Id");

                    b.HasIndex("PublisherId")
                        .IsUnique();

                    b.ToTable("PublisherProfiles");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.PublisherSource", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("pubs_no");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at");

                    b.Property<string>("Provider")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("provider");

                    b.Property<long>("PublisherId")
                        .HasColumnType("bigint")
                        .HasColumnName("publisher_id");

                    b.Property<string>("SourceUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("soruce_url");

                    b.Property<int>("Status")
                        .HasColumnType("int")
                        .HasColumnName("status");

                    b.Property<string>("Type")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("type");

                    b.HasKey("Id");

                    b.HasIndex("PublisherId");

                    b.ToTable("PublisherSources");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Subscription", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("sub_id");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("description");

                    b.Property<string>("Duration")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("duration");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("sub_name");

                    b.Property<double>("Price")
                        .HasColumnType("float")
                        .HasColumnName("sub_price");

                    b.HasKey("Id");

                    b.ToTable("Subscriptions");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Transaction", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("trans_id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<double>("Amount")
                        .HasColumnType("float")
                        .HasColumnName("amount");

                    b.Property<long?>("CampaignId")
                        .HasColumnType("bigint")
                        .HasColumnName("campaign_id");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at");

                    b.Property<int>("Status")
                        .HasColumnType("int")
                        .HasColumnName("payment_status");

                    b.Property<long?>("SubscriptionId")
                        .HasColumnType("bigint")
                        .HasColumnName("subscription_id");

                    b.Property<Guid>("UserCode")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("user_code");

                    b.Property<long>("WalletId")
                        .HasColumnType("bigint")
                        .HasColumnName("wallet_id");

                    b.HasKey("Id");

                    b.HasIndex("CampaignId");

                    b.HasIndex("SubscriptionId");

                    b.HasIndex("UserCode");

                    b.HasIndex("WalletId");

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.User", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("user_id");

                    b.Property<string>("Address")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("address");

                    b.Property<string>("CitizenId")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("citizen_id");

                    b.Property<byte[]>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .IsRequired()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion")
                        .HasColumnName("concurrency_stamp");

                    b.Property<DateTime?>("DateOfBirth")
                        .HasColumnType("datetime2")
                        .HasColumnName("date_of_birth");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)")
                        .HasColumnName("user_email");

                    b.Property<bool?>("EmailConfirmed")
                        .HasColumnType("bit")
                        .HasColumnName("email_confirmed");

                    b.Property<DateTime?>("ExpiryDate")
                        .HasColumnType("datetime2")
                        .HasColumnName("token_expired_date");

                    b.Property<string>("FirstName")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("first_name");

                    b.Property<string>("LastName")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("last_name");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("user_password");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("phone_number");

                    b.Property<string>("RejectReason")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("reject_reason");

                    b.Property<string>("ResetPasswordToken")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("reset_password_token");

                    b.Property<int>("Role")
                        .HasColumnType("int")
                        .HasColumnName("user_role");

                    b.Property<int>("Status")
                        .HasColumnType("int")
                        .HasColumnName("user_status");

                    b.Property<Guid>("UserCode")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("user_code");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("UserCode")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.UserBank", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("ub_id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("AddedDate")
                        .HasColumnType("datetime2")
                        .HasColumnName("added_date");

                    b.Property<long>("BankingNo")
                        .HasColumnType("bigint")
                        .HasColumnName("banking_no");

                    b.Property<string>("BankingProvider")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("banking_provider");

                    b.Property<Guid?>("UserCode")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("user_code");

                    b.HasKey("Id");

                    b.HasIndex("UserCode");

                    b.ToTable("UserBank");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Wallet", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<double>("Balance")
                        .HasColumnType("float")
                        .HasColumnName("balance");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit")
                        .HasColumnName("is_active");

                    b.Property<Guid>("UserCode")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("user_code");

                    b.HasKey("Id");

                    b.HasIndex("UserCode")
                        .IsUnique();

                    b.ToTable("Wallets");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.WalletHistory", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("wh_id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<bool>("BalanceType")
                        .HasColumnType("bit")
                        .HasColumnName("balance_type");

                    b.Property<double?>("CurrentBalance")
                        .HasColumnType("float")
                        .HasColumnName("current_balance");

                    b.Property<long?>("TransactionId")
                        .HasColumnType("bigint")
                        .HasColumnName("transaction_id");

                    b.HasKey("Id");

                    b.HasIndex("TransactionId")
                        .IsUnique()
                        .HasFilter("[transaction_id] IS NOT NULL");

                    b.ToTable("WalletHistories");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.AdvertiserProfile", b =>
                {
                    b.HasOne("ANF.Core.Models.Entities.User", "Advertiser")
                        .WithOne("AdvertiserProfile")
                        .HasForeignKey("ANF.Core.Models.Entities.AdvertiserProfile", "AdvertiserId");

                    b.Navigation("Advertiser");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Campaign", b =>
                {
                    b.HasOne("ANF.Core.Models.Entities.User", "Advertiser")
                        .WithMany("Campaigns")
                        .HasForeignKey("AdvertiserCode")
                        .HasPrincipalKey("UserCode")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ANF.Core.Models.Entities.Category", "Category")
                        .WithMany("Campaigns")
                        .HasForeignKey("CategoryId");

                    b.Navigation("Advertiser");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.CampaignImage", b =>
                {
                    b.HasOne("ANF.Core.Models.Entities.Campaign", "Campaign")
                        .WithMany("Images")
                        .HasForeignKey("CampaignId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Campaign");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Offer", b =>
                {
                    b.HasOne("ANF.Core.Models.Entities.Campaign", "Campaign")
                        .WithMany("Offers")
                        .HasForeignKey("CampaignId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Campaign");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.PostbackData", b =>
                {
                    b.HasOne("ANF.Core.Models.Entities.Offer", "Offer")
                        .WithMany("PostbackData")
                        .HasForeignKey("OfferId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Offer");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.PublisherOffer", b =>
                {
                    b.HasOne("ANF.Core.Models.Entities.Offer", "Offer")
                        .WithMany("PublisherOffers")
                        .HasForeignKey("OfferId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("ANF.Core.Models.Entities.User", "Publisher")
                        .WithMany("PublisherOffers")
                        .HasForeignKey("PublisherCode")
                        .HasPrincipalKey("UserCode")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Offer");

                    b.Navigation("Publisher");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.PublisherProfile", b =>
                {
                    b.HasOne("ANF.Core.Models.Entities.User", "Publisher")
                        .WithOne("PublisherProfile")
                        .HasForeignKey("ANF.Core.Models.Entities.PublisherProfile", "PublisherId");

                    b.Navigation("Publisher");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.PublisherSource", b =>
                {
                    b.HasOne("ANF.Core.Models.Entities.User", "Publisher")
                        .WithMany("AffiliateSources")
                        .HasForeignKey("PublisherId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Publisher");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Transaction", b =>
                {
                    b.HasOne("ANF.Core.Models.Entities.Campaign", "Campaign")
                        .WithMany("Transactions")
                        .HasForeignKey("CampaignId");

                    b.HasOne("ANF.Core.Models.Entities.Subscription", "Subscription")
                        .WithMany("Transactions")
                        .HasForeignKey("SubscriptionId")
                        .OnDelete(DeleteBehavior.NoAction);

                    b.HasOne("ANF.Core.Models.Entities.User", "User")
                        .WithMany("Transactions")
                        .HasForeignKey("UserCode")
                        .HasPrincipalKey("UserCode")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("ANF.Core.Models.Entities.Wallet", "Wallet")
                        .WithMany("Transactions")
                        .HasForeignKey("WalletId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Campaign");

                    b.Navigation("Subscription");

                    b.Navigation("User");

                    b.Navigation("Wallet");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.UserBank", b =>
                {
                    b.HasOne("ANF.Core.Models.Entities.User", "User")
                        .WithMany("UserBanks")
                        .HasForeignKey("UserCode")
                        .HasPrincipalKey("UserCode")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("User");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Wallet", b =>
                {
                    b.HasOne("ANF.Core.Models.Entities.User", "User")
                        .WithOne("Wallet")
                        .HasForeignKey("ANF.Core.Models.Entities.Wallet", "UserCode")
                        .HasPrincipalKey("ANF.Core.Models.Entities.User", "UserCode");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.WalletHistory", b =>
                {
                    b.HasOne("ANF.Core.Models.Entities.Transaction", "Transaction")
                        .WithOne("WalletHistory")
                        .HasForeignKey("ANF.Core.Models.Entities.WalletHistory", "TransactionId")
                        .OnDelete(DeleteBehavior.NoAction);

                    b.Navigation("Transaction");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Campaign", b =>
                {
                    b.Navigation("Images");

                    b.Navigation("Offers");

                    b.Navigation("Transactions");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Category", b =>
                {
                    b.Navigation("Campaigns");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Offer", b =>
                {
                    b.Navigation("PostbackData");

                    b.Navigation("PublisherOffers");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Subscription", b =>
                {
                    b.Navigation("Transactions");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Transaction", b =>
                {
                    b.Navigation("WalletHistory");
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.User", b =>
                {
                    b.Navigation("AdvertiserProfile")
                        .IsRequired();

                    b.Navigation("AffiliateSources");

                    b.Navigation("Campaigns");

                    b.Navigation("PublisherOffers");

                    b.Navigation("PublisherProfile")
                        .IsRequired();

                    b.Navigation("Transactions");

                    b.Navigation("UserBanks");

                    b.Navigation("Wallet")
                        .IsRequired();
                });

            modelBuilder.Entity("ANF.Core.Models.Entities.Wallet", b =>
                {
                    b.Navigation("Transactions");
                });
#pragma warning restore 612, 618
        }
    }
}
