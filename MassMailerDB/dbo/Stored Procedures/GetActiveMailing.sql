	
CREATE proc dbo.GetActiveMailing
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

    select  top 1 M.ID
		,	M.MailStateID
		,	M.Name
		,	M.Sender
		,	M.Body
		,	case 
				when M.MailStateID = @TestSendStateID then '*** ТЕСТ *** ' + M.Subject
				else M.Subject
			end as Subject
    from    Mail M with(nolock)
    where   M.MailStateID = @TestSendStateID or
			(M.MailStateID = @SendStateID and
            M.DateFrom <= @d and
            (M.DateTo is null or DateTo >= @d))
    order by M.DateFrom
end