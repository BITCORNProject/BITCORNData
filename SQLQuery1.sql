select * from userstat where userid = 2081

update userstat set TotalReferrals  = 0 where userid = 2081
update userstat set TotalReferralRewards  = 0 where userid = 2081
update UserReferral set WalletDownloadDate = null where UserId = 2771
update UserReferral set MinimumBalanceDate = null where UserId = 2771
select * from UserReferral where userid = 2771

select * from [user] where UserId >= 2770
select * from UserIdentity where UserId = 5292

update userwallet set balance = 1000 where userId = 2771

update userwallet set balance = 0 where userId = 2081

select * from UserReferral where UserId = 5292

select * from ErrorLogs

select * from referrer where userId = 2081

select * from useridentity where TwitchUsername like  '%bombbot%'

select * from UserIdentity where Auth0Nickname = 'MATTRASTEST'

select * from ReferralTx

update UserIdentity set TwitterId = null where TwitterId =  '1233935434712903680'
update UserIdentity set TwitterUsername = null where  TwitterId =  '1233935434712903680'

update UserReferral set MinimumBalanceDate = null where UserId = 2771
select * from UserReferral where UserId = 2771


