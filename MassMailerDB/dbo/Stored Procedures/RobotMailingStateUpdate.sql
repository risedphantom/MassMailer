
CREATE proc dbo.RobotMailingStateUpdate
as
begin
	declare	@MailStateID int
		,	@NewMailStateID int
		,	@MailStateTest int = 5
		,	@MailStateTestDone int = 6
		,	@MailStateSending int = 15
		,	@MailStateSend int = 50
		,	@MailingID int
		,	@MissionID int
		,	@MissionStateID int
		,	@PlannedCount int
		,	@DoneCount int
		,	@InProgressCount int
		,	@MissionCount int
		,	@SetID int
		,	@MailGroupID int
		,	@MissionStateNew int = dbo.MissionStateGetBySysName('MissionStateNew')
		,	@MissionStateInProgress int = dbo.MissionStateGetBySysName('MissionStateInProgress')
		,	@MissionStateComplete int = dbo.MissionStateGetBySysName('MissionStateComplete')
		,	@MissionStateError int = dbo.MissionStateGetBySysName('MissionStateError')
		,	@User varchar(8000) = 'RobotMailingStateUpdate'
		,	@StatusWaiting int = 0
		,	@StatusInProgress int = 2

	--Get all active mailings
	declare	aCrsr cursor static local for

	select	ID
		,	MailStateID
	from	Mailing
	where	MailStateID in (@MailStateTest, @MailStateSending)
	
	open aCrsr
	fetch next from aCrsr into @MailingID, @MailStateID

	while @@fetch_status = 0
	begin
		--Get new mailing state
		if @MailStateID = @MailStateTest
			set @NewMailStateID = @MailStateTestDone
		else
			set @NewMailStateID = @MailStateSend

		--Get all active missions in current mailing
		declare	bCrsr cursor static local for

		select	ID
			,	State
			,	SetID
			,	MailGroupID
		from	Mission M 
		where	MailingID = @MailingID
				and State = @MissionStateInProgress
		
		open bCrsr
		fetch next from bCrsr into @MissionID, @MissionStateID, @SetID, @MailGroupID
		
		while @@fetch_status = 0
		begin
			select	@PlannedCount = 0
				,	@DoneCount = 0

			if @SetID is null
			begin
				select	@PlannedCount = count(1)
				from	MailGroupUser
				where	MailGroupID = @MailGroupID
			end
			else
			begin
				select	@PlannedCount = count(1)
				from	ClientSet
				where	SetID = @SetID
			end
						
			select	@DoneCount = count(1)
			from	Archive with(nolock)
			where	MissionID = @MissionID

			select	@MissionCount = @DoneCount + count(1)
			from	ActiveQueue with(nolock)
			where	MissionID = @MissionID

			select	@DoneCount = @DoneCount + count(1)
			from	ActiveQueue with(nolock)
			where	MissionID = @MissionID
					and Status <> @StatusWaiting
					and Status <> @StatusInProgress
			
			select	@InProgressCount = count(1)
			from	ActiveQueue with(nolock)
			where	MissionID = @MissionID
					and Status in (@StatusWaiting, @StatusInProgress)

			--Mission completed
			if @DoneCount = @PlannedCount
				exec MissionSetState 
					@ID = @MissionID
				,	@State = @MissionStateComplete
				,	@User = @User
			--Mission error
			else if (@InProgressCount = 0) and (@MissionCount <> @PlannedCount)
				exec MissionSetState 
					@ID = @MissionID
				,	@State = @MissionStateError
				,	@User = @User
			
			fetch next from bCrsr into @MissionID, @MissionStateID, @SetID, @MailGroupID
		end

		close bCrsr
		deallocate bCrsr

		--Now if all missions completed - complete mailing
		if not exists(	select	top 1 1
						from	Mission
						where	MailingID = @MailingID
								and State in (@MissionStateNew, @MissionStateInProgress) )
			exec MailSetState 
				@ID = @MailingID
			,	@MailStateId = @NewMailStateID
			,	@UserName = @User 
				
		fetch next from aCrsr into @MailingID, @MailStateID
	end

	close aCrsr
	deallocate aCrsr
end