
CREATE proc dbo.MailLst
    @MailStateID int,
    @DateFrom datetime,
    @DateTo datetime,
	@BranchID int = 5
as
begin
    select  M.*
		,	MailState = MS.Name
		,	@BranchID as BranchID
    from    Mailing M with(nolock)
            join MailState MS with(nolock) on (MS.ID = M.MailStateID)
    where   MailStateID = @MailStateID
end