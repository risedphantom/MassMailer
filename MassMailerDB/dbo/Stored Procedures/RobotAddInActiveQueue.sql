
CREATE proc [dbo].[RobotAddInActiveQueue]
as
begin
    declare	@MissionStateInProgress int = dbo.MissionStateGetBySysName('MissionStateInProgress')
		,	@MissionStatePlanned int = dbo.MissionStateGetBySysName('MissionStatePlanned')
		,	@MailStateProcess int = 15
		,	@SetID int
		,	@MissionID int
		,	@MailingID int
		,	@TemplateID int
		,	@AddressFrom varchar(8000)
		,	@Subject varchar(8000)
		,	@Priority int
		,	@WaitingStateID int = 0
		,	@UnsubscribeTypeID int 

	select	@UnsubscribeTypeID = TokenTypeID
	from	ClientWH.dbo.TokenType
	where	TokenTypeCode = 'TokenClientAdUnsubscribe'

    declare	aCrsr cursor static local for
    
    select	M.SetID
		,	M.ID 
		,	MM.TemplateID
		,	MM.AddressFrom
		,	MM.Priority
		,	MM.Subject
		,	M.MailingID
	from	Mission M
			join Mailing MM on M.MailingID = MM.ID
	where	M.State = @MissionStatePlanned
			and M.SetID is not null
	
	open aCrsr

	fetch next from aCrsr into @SetID, @MissionID, @TemplateID, @AddressFrom, @Priority, @Subject, @MailingID
    
    while @@fetch_status = 0
    begin
        
        begin tran
			insert	ActiveQueue (TemplateID, XMLData, AddressFrom, AddressTo, Subject, Priority, Status, MissionID, ExternalOwnerID, SendMoment, AddMoment, Host)
			select	@TemplateID as TemplateID
				,	(select	CXML.ClientID
						,	ClientGUID
						,	CM.SourceClientID as TozonClientID
						,	RegistrationDate
						,	LastVisitDate
						,	PromoStateID
						,	FirstName
						,	MiddleName
						,	LastName
						,	ContactPhone1
						,	TokenGUID
						,	[Login]
					from	ClientWH.dbo.Client CXML with(nolock)
							join ClientWH.matching.Client CM with(nolock) on CM.ClientID = CXML.ClientID 
							left join ClientWH.dbo.Token T with(nolock) on T.ClientID = CXML.ClientID 
					where	CXML.ClientID = CS.ClientID
							and T.TokenTypeID = @UnsubscribeTypeID
					for xml path('Client'), type, ELEMENTS XSINIL) as XMLData
				,	@AddressFrom as AddressFrom
				,	C_xml.Email as AddressTo
				,	@Subject as Subject
				,	@Priority as Priority
				,	@WaitingStateID as Status
				,	@MissionID as MissionID
				,	CM.SourceClientID as ExternalOwnerID
				,	null as SendMoment
				,	getdate() as AddMoment
				,	null as Host
			from	ClientSet CS
					left join ClientWH.dbo.Client C_xml on C_xml.ClientID = CS.ClientID
					left join ClientWH.matching.Client CM on CM.ClientID = C_xml.ClientID
			where	CS.SetID = @SetID

			exec	MissionSetState 
					@ID = @MissionID
				,	@State = @MissionStateInProgress
				,	@User = 'RobotAddInActiveQueue'

			exec	MailSetState 
					@ID = @MailingID
				,	@MailStateID = @MailStateProcess
				,	@UserName = 'RobotAddInActiveQueue'
        commit

        fetch next from aCrsr into @SetID, @MissionID, @TemplateID, @AddressFrom, @Priority, @Subject, @MailingID
    end

	close aCrsr
	deallocate aCrsr
end