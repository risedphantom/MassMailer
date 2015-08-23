
CREATE proc dbo.ClientToAddInMailQueueIns
    @MailID int,
    @SelectID int,
	@User varchar(8000) = 'ClientToAddInMailQueueIns'
as
begin
	declare @TemplateID bigint
		,	@MissionID int
		,	@MissionStatePlanned int = dbo.MissionStateGetBySysName('MissionStatePlanned')
	
	select	@TemplateID = TemplateID
	from	Mailing M 
	where	M.ID = @MailID

	begin tran
		insert	Mission (MailingID, State, StateChangeMoment, [User], Test, SetID, MailGroupID, ListID)
		values	(@MailID, @MissionStatePlanned, getdate(), @User, 0, @SelectID, null, cast(newid() as varchar(255)) + '.ozon.travel')
	
		set @MissionID = SCOPE_IDENTITY()	

		insert	MissionLog (MissionID, State, StateChangeMoment, [User])
		values	(@MissionID, @MissionStatePlanned, getdate(), @User)
	commit
end