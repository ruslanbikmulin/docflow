CREATE TABLE [dbo].[DocumentModels] (
    [Id] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](max),
    [Content] [varbinary](max),
    CONSTRAINT [PK_dbo.DocumentModels] PRIMARY KEY ([Id])
)
CREATE TABLE [dbo].[__MigrationHistory] (
    [MigrationId] [nvarchar](150) NOT NULL,
    [ContextKey] [nvarchar](300) NOT NULL,
    [Model] [varbinary](max) NOT NULL,
    [ProductVersion] [nvarchar](32) NOT NULL,
    CONSTRAINT [PK_dbo.__MigrationHistory] PRIMARY KEY ([MigrationId], [ContextKey])
)
INSERT [dbo].[__MigrationHistory]([MigrationId], [ContextKey], [Model], [ProductVersion])
VALUES (N'201711281250051_AddCompanyMigration', N'Docflow.Migrations.Configuration',  0x1F8B0800000000000400CD57DB6EDB38107D5FA0FF20F0A90BA466D2BE7403B9456A2745B071525469DF6969E410A5489597ACF56DFBB09FB4BFB043DD4DDB89932D8A22402051336766CEDCE87FFFFE277EBF2E44740FDA7025A7E464724C2290A9CAB85C4D89B3F9ABB7E4FDBB17BFC5E759B18EBE76726FBC1C6A4A332577D696A7949AF40E0A6626054FB5322AB793541594658ABE3E3EFE839E9C504008825851147F76D2F202EA177C9D299942691D130B958130ED397E496AD4E89A15604A96C294CC559A0BF5D7647E76353947145BA1B685B525D199E00C1D4A40E42462522ACB2CBA7BFAC54062B592ABA4C403266EAB12502E67C2401BC6E9207E6844C7AF7D447450ECA05267AC2A9E0878F2A6A58886EACF229AF41422890D4D3EEA9AC89A435780B435DB240A2D9ECE84F6D29B6437A9996CE81E45ADC4515F1B5842FEEF289A39619D86A904673543D14F6E2978FA2754B7EA1BC8A974428CDD4447F1DBC6011E7DD2AA046DABCF90B7CE5F6624A29B7A3454ECD5463A4D441F1DC7E76BB4CD9602FA22A00FAAFBFF1D005612F60689166C7D057265EFA6041F4974C1D79075272DEA17C9B19550C96AF7A891BA8CA5EDEC7CE092E9EA303B63E0980EE9DE2E026F8471093AA884BE87B66A019BA72D07D3C6B11945839B800D100D8906479AA69C0475B7CBEDDEC16106D0660874C382EE9916F1829525A666343DDA93286946C7EC55F2F4662A1A0C9A9A1D3DD57BDB5BB24AB315045F7D7233B8E0DAD839B36CC97CD26659B12516A6630FD59DB590F1B0798604741AFEB9D7DA3D47832C059803AB1718A817AB6386DEB770BA6CE9D7539D09A677F4E84C0957C87D7DFE9076D3A263FDE6E47084BEFFC620FDE1364E4C032242F6E916FDC1D40AF3F9503B8422BDF5BE2D82F28FDB527C7CA36ED56623422224E89E67BE2E93CA5828265E60927C1733C16B523A8105933C07639BC14EB075DE06DBF8D7D98CD4984C3C653DFEF415E524FFEE007945BF720EFA7FAD2B79CF747AC7F4CB82AD7F1F233D632521D4B2DE4A07803D6D0D6D8FC7C3964CBB481E5B344D414F49B654184BE3F2868879F632DAEEB2988E6FB7F11C0C5F0D10FEAE2B21F5E53B8076329732575D0A30D8B1479D4890A10558962167671A6B85A5163FA7604C7D43F9CA844391F36209D9A5BC71B674F6CC182896A21AC71BD387EDD71B77D3E7F8A6F46FE64784806E720C016EE407C745D6FB7DB1E36EB307C297CF47C0F37A2CE00D0DE156558F74ADE481402D7D732841FAFEBB85A21408666E64C2EEE139BEE1FDE90A562CADBA61B91FE4F1446CD21ECF395B6956981663D0F7BFD8A8FFC9F6EE3FF9DF46E2E40D0000 , N'6.2.0-61023')
