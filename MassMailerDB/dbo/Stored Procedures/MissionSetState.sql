
CREATE proc dbo.MissionSetState
    @ID int, --ID рассылки
    @State int,
	@User varchar(8000) = 'MassMailerControl'
as
begin
	begin tran
		update	Mission
		set		State = @State
			,	StateChangeMoment = getdate()
			,	[User] = @User
		where	ID = @ID

		insert	MissionLog (MissionID, State, StateChangeMoment, [User])
		values	(@ID, @State, getdate(), @User)
	commit
end