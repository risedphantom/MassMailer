	
CREATE proc [dbo].[SegmentLst]
    @DateFrom datetime,
    @DateTo datetime
as
begin
	create table #ClientSets (SetID int, Cnt int)

    set @DateFrom = dbo.TruncTime(@DateFrom)
    set @DateTo = dateadd(day, 1, dbo.TruncTime(@DateTo))

	insert #ClientSets(SetID, Cnt)
	select	SetID
		,	count(ClientID) as Cnt
	from	ClientSet CS with(nolock)
	group by SetID
	
	select	S.*
		,	CS.Cnt
	from	#ClientSets CS	
			join Sets S on CS.SetID = S.ID
	where   S.Date between @DateFrom and @DateTo
	order by S.ID desc
end