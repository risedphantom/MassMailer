	
CREATE proc [dbo].[MailLstForSend]
as
begin
    declare @d datetime = GetDate()
		,	@TestSendStateID int
		,	@SendStateID int
		
	select	@TestSendStateID = ID
	from	MailState
	where	Name = 'Идет тестовая рассылка'
	
	select	@SendStateID = ID
	from	MailState
	where	Name = 'Идет рассылка'		

    select  *
    from    Mail with(nolock)
    where   MailStateID = @TestSendStateID or
			(MailStateID = @SendStateID and
            DateFrom <= @d and
            (DateTo is null or DateTo >= @d))
    order by DateFrom
end