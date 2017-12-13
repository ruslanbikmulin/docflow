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

CREATE TABLE [dbo].[UploadEntityTables] (
    [ID] [int] NOT NULL IDENTITY,
    [UploadPath] [nvarchar](max),
    [UploadStartDate] [datetime] NOT NULL,
    [Email] [nvarchar](max),
    [FilePath] [nvarchar](max),
    [UploadStatus] [int] NOT NULL,
    [ContractCount] [int] NOT NULL,
    [ErrorCount] [int] NOT NULL,
    [ZippedCount] [int] NOT NULL,
    [UploadEndDate] [datetime] NOT NULL,
    [UserNameStart] [nvarchar](max),
    [UserNameCancel] [nvarchar](max),
    CONSTRAINT [PK_dbo.UploadEntityTables] PRIMARY KEY ([ID])
)
CREATE TABLE [dbo].[UploadProgressTables] (
    [ID] [bigint] NOT NULL IDENTITY,
    [ProcessStatus] [int] NOT NULL,
    [ErrorCode] [int] NOT NULL,
    [UploadEntityTable_ID_ID] [int],
    CONSTRAINT [PK_dbo.UploadProgressTables] PRIMARY KEY ([ID])
)
CREATE INDEX [IX_UploadEntityTable_ID_ID] ON [dbo].[UploadProgressTables]([UploadEntityTable_ID_ID])
ALTER TABLE [dbo].[UploadProgressTables] ADD CONSTRAINT [FK_dbo.UploadProgressTables_dbo.UploadEntityTables_UploadEntityTable_ID_ID] FOREIGN KEY ([UploadEntityTable_ID_ID]) REFERENCES [dbo].[UploadEntityTables] ([ID])
INSERT [dbo].[__MigrationHistory]([MigrationId], [ContextKey], [Model], [ProductVersion])
VALUES (N'201711291347082_AutomaticMigration', N'Docflow.Migrations.Configuration',  0x1F8B0800000000000400D55BDB8EDB36107D2FD07F10F4D4165BCBDE4D9A746137D8D87160347B41BC498BBE04B4447B894A9442519B358A7E591FFA49FD850E75332F9275B1374D51A0F0929C4372E6CC683864FEF9EBEFF18B87C0B7EE318B494827F66830B42D4CDDD0237433B113BEFEFEB9FDE2A7AFBF1ABFF28207EB7D31EE4C8C03491A4FEC3BCEA373C789DD3B1CA0781010978571B8E603370C1CE485CEE970F8A3331A3918206CC0B2ACF1DB847212E0F40FF8731A5217473C41FE65E8613FCEDBA16799A25A5728C071845C3CB167A1BBF6C34F83D9C59BC12B40E15B90E6F881DBD6854F102C6889FDB56D214A438E382CF7FC5D8C979C8574B38CA001F9B7DB08C3B835F2639C6FE37C37BCED8E86A76247CE4EB080729398874147C0D159AE224717EFA568BB542128315393D875AAC854874980294FB56D5BFA8CE7539F89D1AAB233D30C14D9132B1F715272032824FE3BB1A689CF13862714279C21187A93AC7CE2FE8CB7B7E1EF984E68E2FBF23261A1D0A73440D30D0B23CCF8F62D5EE78B5F78B6E5A8728E2E588A4932D98E5E27047E5FC1DC68E5E39204CE5E71F1FF02009804BE615B97E8E10DA61B7E37B1E1A76DCDC903F68A961CF51D25E04A20C459D238494A63CA8B795E128AD8B6DD3C32F0D8D9997B2F09DE457E88BCBC5928A323110CF9FF840CB31E6498153A5E507E765AC10649034B1E32FC1A53CC10C7DE0DE21C332A0885D38D371935531288DD3D3A7FB2A9961C313E83B516F389DFB744F0B723E95F0588F88FBEEA39F1F1E7550F4FE27232F8520CD48E8E4A124ECB90CBA761B273DD3A5A35E89BB1901D01E7371245D83B0250E1E0DE51E8041F61267EA5047D7C6BE7B34D116416C76671C7100B4BDB301CC7FD83AC82F0BF0CB33F3C79D4300B7FBAA09F0AEFD67AFAF9A487154CA9B509EF0ADD934DBAC51AE72ABF9E1F84BEDE623F1D1CDF9148094F0A033E540BCF5918BC0DFD6ADE55CA7C58860973C5E6C28E82B7886D306FEB16496038456190453CF7D166978D377B87808B95A87DB04780B37B98F95B080E72C0544D7989831566F926D22896C692F7C84FA06168985E199F2D571618ED1710515C1E7EBA7FF80D4A62EC95A3CFF68F9E134A806212FC93FD02591895059E36088441E4632E2DE987363348E39F99E4CA68B4875A9AB3B7E7D6721B731C0C7EC12B35609C58BB9E1DB99EB425D76A78B67EBA7EB61E8DBCA74374867A100DCEB4F7B82D6996770917AC9C859F685BE60819657C03776E310BE068229BF6490F534931B46F082821FE03FF2FF2BEAB90CF21D3F2DAC681426E96AE22CDABDA9976B6BA70052F6724CED28876D6CDC348AAAAB6165EBA888AB4DCD85B4390B8819078BD16D2B121DA102EC47499A755EFB221785C61FE2964BFABBB7CD61091C1E46B42B1A74A3D6FC3E58B380E5D9292B33ED3ABFE4AAB2B82E4DAEAFBC9D64F305A9A7909C42711900C8427F67786327A4C5D7CF48DA99522823AF1703018E92A95D467A6CFC24310D8A5247E5E6F2AAB7C4678805C3F8F10719EADEB3B15B84BCC3544F842EC12967C3F5A65CCD0998A54B17B03B1624C2B54CD9C35B8DA280D5952F45EF89A845212EF9A8CEA478A03885EAAA74633CE11E62A986DCCB5CF6E8EAADE8A4CB8E4F2AEA0ED6415EDA2F2EDD494BEC797280DDC52293C6FB196591D7CFAFDB27B6538C8301C37AE281097AB2D67824319DA60AD577CC43C3C272C167526B442E29B3DF5026398EEB935AC2F66D39DD3346BE1018584F85D4A555F0A680EAD61EEB43A878D8A61E99EB1448306F9F48A02F98855149CA7A19F04B4AE68BD4F3AAB37CBF2594B7B84B2982C83948D26CED8D114A16BDF31D4AF39836ECF56D6369DEC38566FC46D61F91618B5D69F69D637CA27FBA4E59AB18C22B7774593CAC226A4D4D91E372F0FCB6879537B8C5DF15786D9B5F6D8657AF0ABDC62DAD3CD83A4CAAEEE475257079D49255E4571527B7B34A5D02BC3291D5D7558D67B4D25965D1D30D5AAAF82A97675C72C6ABB55A045DF1716E9D4D4E598B16E2F72EB68D780F238F14EABDAC8405A57674FF370A5A379951C7E6C66A8E962233D6A92F29E94A841EB4103915F577FB15AE7D9A6EA5BD1AA12B28A6BC212E51A8FB8FCFC48D273F99D570AE6F4485A6458C4E2A2A1AC90F5508A7E5A31196A1C5AF421A57F948717ED9032CE0F0CCD8F788C134436C4B64015F7C413A787BCF02A060C961FFDA94FD2D4B51870892859E39867553CFB74383AD51E007D398F719C38F6FC2E2F72FADCD01DF42A26A1E46382497ADBB626981DF44286DE23E6DE21F64D801EBE95917ABC8201A855FA10A605D8012F5F3EFB9528115B6CBCF4EC75612FBFA638C814350F4A3CF8CD0F7E5072D0CAF44723C7D9A674759C9AE7184F41FA00990F41FAA0543C03E90353F908A437052A1F811C66BDCA871E1D200F7ACCF1D9E3C68A6C1E2574543EA03880BCBB17138790CEC8692448E31A60413DFC30B1FF4831CEADC5AFD539DC6276625D33C820CEADA1F5673F6E3CDAFD8F526E3EEC9AA5A9107EF80D12701A33C137E443EC8B21F811F3CC74C308754984FCDA7D9AD9741BA711C629B1F59E198E3015AEB067FB6DA6ED70E028A7D45CBB49491D6FC3CCD27BBBBBAEFC3EABE9BE2B4BC321C0AF42E047E616CA10E3107EAC3BB1AA998D616D67EF757756BF0265A0B9862FF796ADC20AEDEEB69A8347B75BB9E3DC9599C74BF02FE95F9280DFC764B3831007688A5DC5B3CA310BBA0E0B8FD756540CD1BE4A979823C87CD005834312247AF937537E15F52A58616F41AF131E251CB68C8395AF3C5E148162DFFCE985A0BAE6F175943E093CC616609944246FD7F46542FCDDF38C79C5BF23A8811011284F3C842DB9484036DB12E92AA42D8172F59581F31607910F60F1355D22E9CD5387B54132F8066F90BB2DAA04F520CD8650D53E9E11B4612888738C9D3CFC091CF682879FFE053BC5566250350000 , N'6.2.0-61023')

