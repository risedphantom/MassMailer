
CREATE proc [dbo].[MailStateLstForChange]
    @MailStateID int
as
begin
    select  MST.*,
            MailState = MS.Name
    from    MailStateTransfer MST
            join MailState MS with(nolock) on (MS.ID = MST.ToMailStateID)
    where   FromMailStateID = @MailStateID and
            Active = 1
    order by MS.ID
end