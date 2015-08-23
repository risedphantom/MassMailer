	
CREATE proc dbo.MailLstForMonitor
    @DateFrom datetime,
    @DateTo datetime
as
begin
	declare @ReadyStateID int
		,	@CompleteStateID int
		,	@BranchID int = 5

	set @DateFrom = dbo.TruncTime(@DateFrom)
    set @DateTo = dateadd(day, 1, dbo.TruncTime(@DateTo))
    
    --Fill states
    select	@ReadyStateID = ID
    from	MailState
    where	Name = 'Письмо готово к рассылке'
    
    select	@CompleteStateID = ID
    from	MailState
    where	Name = 'Рассылка выполнена'
    	
	select  M.*
		,	MS.Name as MailState
		,	@BranchID as BranchID
		,	case 
				when MS.ID = @CompleteStateID then M.StateChangeMoment
				else null
			end as DateFrom
		,	null as DateTo
	from    Mailing M with(nolock)
			join MailState MS with(nolock) on (MS.ID = M.MailStateID)
	where   (MailStateID between @ReadyStateID and @CompleteStateID) and
			StateChangeMoment >= @DateFrom and 
			StateChangeMoment < @DateTo
end