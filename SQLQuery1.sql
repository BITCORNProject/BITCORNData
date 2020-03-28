select * from userstat where userid = 2651

update userstat set TotalReferralRewardsUsdt = 0.000001 where userid = 5395

select * from UserReferral where userid = 5421

select * from [UserIdentity] where UserId >= 5413
select * from UserIdentity where UserId = 2651

update userwallet set balance = 1040 where userId = 5435
update UserReferral set MinimumBalanceDate = null

select * from UserReferral where userid = 5383
update userwallet set balance = 1000000 where userId = 2081

update userwallet set balance = 0 where userId = 2081

select * from UserReferral where UserId = 5421

select * from ErrorLogs

select * from referrer where userId = 5423

select * from useridentity where TwitchUsername like  '%bombbot%'

select * from UserIdentity where Auth0Nickname like 'hannahsowers+8'




update UserIdentity set TwitterId = null where TwitterUsername =  'jasonsowers14'
update UserIdentity set TwitterUsername = null where  TwitterUsername =  'jasonsowers14'

update UserReferral set MinimumBalanceDate = null where UserId = 2771
update UserReferral set SyncDate = null where UserId = 5320
update UserReferral set ReferrerBonus = 0 where UserId = 5320
update UserReferral set Bonus = 0 where UserId = 5320

update userstat set TotalReferrals  = 500 where userid = 5433
update userstat set TotalReferralRewardsCorn  = 0 where userid = 2081
update userstat set TotalReferralRewardsUsdt  = 0 where userid = 2081
update Referrer set tier  = 3 where userid = 5423
delete from referraltx
select * from Referraltx
where UserId = 5423

select YtdTotal, UserStat.TotalReferrals, UserStat.TotalReferralRewardsUSdt, UserStat.TotalReferralRewardsCorn, UserWallet.Balance
from [Referrer] 
join userStat on UserStat.UserId = Referrer.UserId
join UserWallet on UserStat.UserId = UserWallet.UserId
where UserStat.UserId = 5355


select * from ErrorLogs where id > 280


