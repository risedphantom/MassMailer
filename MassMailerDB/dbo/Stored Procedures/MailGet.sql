
CREATE proc dbo.MailGet
    @ID int
as
begin
    select  M.Name
		,	M.ID
		,	M.Subject
		,	M.MailStateID
		,	M.AddressFrom as Sender
        ,	MailState = MS.Name
		,	T.Body
		,	'' as BodyText
		,	5 as BranchID
		,	1 as ShowInClientNotification
		,	0 as SendForNotSpam
		,	null as EmailTo
		,	null as DateTo
		,	M.StateChangeMoment as DateFrom
    from    Mailing M with(nolock)
            join MailState MS with(nolock) on (MS.ID = M.MailStateID)
			join Template T on T.ID = M.TemplateID
    where   M.ID = @ID
end