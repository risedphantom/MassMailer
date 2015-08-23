	
CREATE proc dbo.MailQueueLstStat
    @MailID int
as
begin
    declare	@StatusOK int = 250
		,	@StatusWaiting int = 0
		,	@StatusInProgress int = 2

	if OBJECT_ID('tempdb..#stats') is null
		create table #stats (
				MissionID		int primary key
			,	Total			bigint
			,	Success			bigint
			,	Waiting			bigint
			,	InProgress		bigint
			,	Error			bigint)
	else
		truncate table #stats

	insert into #stats
	select	A.MissionID
		,	count(1) as Total
		,	sum(case A.Status when @StatusOK then 1 else 0 end) as Success
		,	sum(case A.Status when @StatusWaiting then 1 else 0 end) as Waiting
		,	sum(case A.Status when @StatusInProgress then 1 else 0 end) as InProgress
		,	sum(case when A.Status in (@StatusOK, @StatusWaiting, @StatusInProgress) then 0 else 1 end) as Error	
	from	Archive A with(nolock)
	where	A.MailingID = @MailID
	group by A.MissionID

	merge #stats as target
	using (	select	AQ.MissionID
				,	count(1) as Total
				,	sum(case AQ.Status when @StatusOK then 1 else 0 end) as Success
				,	sum(case AQ.Status when @StatusWaiting then 1 else 0 end) as Waiting
				,	sum(case AQ.Status when @StatusInProgress then 1 else 0 end) as InProgress
				,	sum(case when AQ.Status in (@StatusOK, @StatusWaiting, @StatusInProgress) then 0 else 1 end) as Error	
			from	ActiveQueue AQ with(nolock)
					join Mission M on AQ.MissionID = M.ID
			where	M.MailingID = @MailID
			group by AQ.MissionID) as source(MissionID, Total, Success, Waiting, InProgress, Error)
	on target.MissionID = source.MissionID
	when matched then
		update set	Total = target.Total + source.Total
				,	Success = target.Success + source.Success
				,	Waiting = target.Waiting + source.Waiting
				,	InProgress = target.InProgress + source.InProgress
				,	Error = target.Error + source.Error
	when not matched then
		insert (MissionID, Total, Success, Waiting, InProgress, Error)
		values (source.MissionID, source.Total, source.Success, source.Waiting, source.InProgress, source.Error);	

	select	S.MissionID as Mission
		,	S.Total
		,	S.Success
		,	S.Waiting
		,	S.InProgress
		,	S.Error
		,	M.Test
		,	MS.Name as State
		,	M.StateChangeMoment	
	from	#stats S
			join Mission M on S.MissionID = M.ID
			join MissionState MS on M.[State] = MS.ID
	order by S.MissionID desc 
end