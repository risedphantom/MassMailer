	
CREATE proc [dbo].[MailLstForDelivery]
    @MailStateID int,
    @DateFrom datetime,
    @DateTo datetime
as
begin
	set @DateFrom = dbo.TruncTime(@DateFrom)
    set @DateTo = dateadd(day, 1, dbo.TruncTime(@DateTo))
    	
	select  M.*,
			MailState = MS.Name,
			MailGroup = MG.Name
	from    Mail M with(nolock)
			join MailState MS with(nolock) on (MS.ID = M.MailStateID)
			left join MailGroup MG with(nolock) on (MG.ID = M.DefaultMailGroupID)
	where   (MailStateID = @MailStateID or isNull(@MailStateID, 0) = 0) and
			StateChangeMoment >= @DateFrom and 
			StateChangeMoment < @DateTo
end