select * from userstat where userid = 2081

select * from UserReferral where userid = 2771

select * from [UserIdentity] where UserId >= 5290
select * from UserIdentity where UserId = 5294

update userwallet set balance = 1000 where userId = 5310
update UserReferral set MinimumBalanceDate = null

select * from UserReferral where userid = 5313
update userwallet set balance = 1020 where userId = 5314

update userwallet set balance = 0 where userId = 2081

select * from UserReferral where UserId = 5294

select * from ErrorLogs

select * from referrer where userId = 2081

select * from useridentity where TwitchUsername like  '%bombbot%'

select * from UserIdentity where Auth0Nickname = 'CORN2MOON'

select * from ReferralTx
update UserIdentity set TwitterId = null where TwitterUsername =  'JasonSowers14'
update UserIdentity set TwitterUsername = null where  TwitterUsername =  'JasonSowers14'

update UserReferral set MinimumBalanceDate = null where UserId = 2771

update userstat set TotalReferrals  = 0 where userid = 2081
update userstat set TotalReferralRewards  = 0 where userid = 2081
update Referrer set YtdTotal  = 0 where userid = 2081


select YtdTotal, UserStat.TotalReferrals, UserStat.TotalReferralRewards 
from [Referrer]
join userStat on UserStat.UserId = Referrer.UserId
where UserStat.UserId = 2081



