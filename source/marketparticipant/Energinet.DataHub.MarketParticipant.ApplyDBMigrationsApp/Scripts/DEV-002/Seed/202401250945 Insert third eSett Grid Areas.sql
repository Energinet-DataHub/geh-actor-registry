﻿INSERT INTO [dbo].[GridArea] ([Id], [Code], [Name], [PriceAreaCode], [ValidFrom], [FullFlexDate], [ChangedByIdentityId])
VALUES
    (NEWID(), '512', 'Netområde 512', 1, CONVERT(datetime, '21-02-2018 23:00:00', 105), CONVERT(datetime, '31-12-2019 23:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '532', 'Netområde 532', 1, CONVERT(datetime, '20-08-2018 22:00:00', 105), CONVERT(datetime, '30-06-2019 22:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '533', 'Netområde 533', 1, CONVERT(datetime, '23-04-2018 22:00:00', 105), CONVERT(datetime, '30-04-2020 22:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '584', 'Netområde 584', 1, CONVERT(datetime, '31-03-2020 22:00:00', 105), CONVERT(datetime, '30-04-2020 22:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000')
