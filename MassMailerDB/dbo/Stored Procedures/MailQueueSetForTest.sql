
CREATE proc [dbo].[MailQueueSetForTest]
    @ID int, --ID рассылки
    @MailGroupID int,
	@User varchar(8000) = 'MassMailerControl'
as
begin
	declare @TemplateID bigint
		,	@AddressFrom varchar(50)
		,	@AddressTo varchar(50)
		,	@Subject varchar(8000)
		,	@Priority int
		,	@Status int
		,	@MissionID int
		,	@MissionStateInProgress int = dbo.MissionStateGetBySysName('MissionStateInProgress')
		,	@WaitingStateID int = 0
		,	@UnsubscribeTypeID int 
	
	select	@TemplateID = TemplateID
		,	@AddressFrom = AddressFrom
		,	@Subject = '***ТЕСТ*** ' + Subject
		,	@Priority = 4500 --Повышаем приоритет тестовых рассылок
	from	Mailing M 
	where	M.ID = @ID 

	select	@UnsubscribeTypeID = TokenTypeID
	from	ClientWH.dbo.TokenType
	where	TokenTypeCode = 'TokenClientAdUnsubscribe'

	begin tran
		insert	Mission (MailingID, State, StateChangeMoment, [User], Test, SetID, MailGroupID, ListID)
		values	(@ID, @MissionStateInProgress, getdate(), @User, 1, null, @MailGroupID, cast(newid() as varchar(255)) + '.ozon.travel')
	
		set @MissionID = SCOPE_IDENTITY()	

		insert	MissionLog (MissionID, State, StateChangeMoment, [User])
		values	(@MissionID, @MissionStateInProgress, getdate(), @User)


		insert	ActiveQueue (TemplateID, XMLData, AddressFrom, AddressTo, Subject, Priority, Status, MissionID, ExternalOwnerID, SendMoment, AddMoment, Host)
		select	@TemplateID as TemplateID
			,	(select	CXML.ClientID
					,	CM.SourceClientID as TozonClientID
					,	ClientGUID
					,	RegistrationDate
					,	LastVisitDate
					,	PromoStateID
					,	FirstName
					,	MiddleName
					,	LastName
					,	ContactPhone1
					,	TokenGUID
					,	[Login]
				from	ClientWH.dbo.Client CXML
						join ClientWH.matching.Client CM on CM.ClientID = CXML.ClientID
						left join ClientWH.dbo.Token T on T.ClientID = CXML.ClientID
				where	CXML.ClientID = MGU.ClientID
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
		from	MailGroupUser MGU
				left join ClientWH.dbo.Client C_xml on C_xml.ClientID = MGU.ClientID
				left join ClientWH.matching.Client CM on CM.ClientID = C_xml.ClientID
		where   MailGroupID = @MailGroupID

	commit
end