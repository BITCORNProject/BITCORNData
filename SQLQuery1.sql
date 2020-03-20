select * from userstat where userid = 2081

select * from UserReferral where userid = 2771

select * from [UserIdentity] where UserId >= 5320
select * from UserIdentity where UserId = 5355

update userwallet set balance = 0 where userId = 5355
update UserReferral set MinimumBalanceDate = null

select * from UserReferral where userid = 5322
update userwallet set balance = 1000000 where userId = 2081

update userwallet set balance = 0 where userId = 2081

select * from UserReferral where UserId = 5320

select * from ErrorLogs

select * from referrer where userId = 5330

select * from useridentity where TwitchUsername like  '%bombbot%'

select * from UserIdentity where Auth0Nickname = 'CORN2MOON'


update UserIdentity set TwitterId = null where TwitterUsername =  'JasonSowers14'
update UserIdentity set TwitterUsername = null where  TwitterUsername =  'JasonSowers14'

update UserReferral set MinimumBalanceDate = null where UserId = 2771
update UserReferral set SyncDate = null where UserId = 5320
update UserReferral set ReferrerBonus = 0 where UserId = 5320
update UserReferral set Bonus = 0 where UserId = 5320

update userstat set TotalReferrals  = 0 where userid = 2081
update userstat set TotalReferralRewardsCorn  = 0 where userid = 2081
update userstat set TotalReferralRewardsUsdt  = 0 where userid = 2081
update Referrer set YtdTotal  = 0 where userid = 2081
delete from referraltx
select * from ReferralTx

select userstat.userid,  YtdTotal, UserStat.TotalReferrals, UserStat.TotalReferralRewardsUSdt, UserStat.TotalReferralRewardsCorn, UserWallet.Balance
from [Referrer] 
join userStat on UserStat.UserId = Referrer.UserId
join UserWallet on UserStat.UserId = UserWallet.UserId
where UserStat.UserId = 2081


select * from ErrorLogs where id > 280


