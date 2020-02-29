select * from userstat where userid = 2081

update userstat set TotalReferrals  = 0 where userid = 2081
update userstat set TotalReferralRewards  = 0 where userid = 2081

select * from UserReferral where userid = 2747

select * from [userwallet] where UserId >= 2740
select * from [userwallet] where UserId = 2081

update userwallet set balance = 1000 where userId = 2755

update userwallet set balance = 0 where userId = 2081

select * from ErrorLogs

select * from referrer where userId = 2478


