
CREATE proc dbo.GetBatchFromQueue
		@BlockSize int
	,	@Mode varchar(100) = null
	,	@Host varchar(128)
as
begin
	--Статусы "В обработке" и "Ожидает отправки"
	declare @InProgressStateID bigint = 2
		,	@WaitingStateID bigint = 0
	
	--Set working mode
	declare @Priority table(ID int primary key)
	if @Mode = 'massmail'
	begin
		insert	@Priority(ID)
		select	ID
		from	Priority
		where	ID > 1000
	end
	else if @Mode = 'service'
	begin
		insert	@Priority(ID)
		values	(1000)
	end
	else
	begin
		insert	@Priority(ID)
		select	ID
		from	Priority
	end		

	declare @Mails table(ID bigint primary key)

	-- Lock data
	set transaction isolation level repeatable read

	update	Q
	set		Status = @InProgressStateID
		,	Host = @Host
	output	deleted.ID into	@Mails
	from	(
			select	top(@BlockSize) AQ.ID 
				,	AQ.Status
				,	AQ.Host
			from	ActiveQueue AQ with(rowlock, xlock, readpast)
					join @Priority P on AQ.Priority = P.ID
			where	Status = @WaitingStateID
			order by Priority, AQ.ID
			) Q 

	set transaction isolation level read committed
		
	select	AQ.ID
		,	AQ.TemplateID
		,	AQ.XMLData
		,	AQ.AddressFrom
		,	AQ.AddressTo		--must be "," delimited string
		,	AQ.AddressCC		--must be "," delimited string
		,	AQ.Subject
		,	AQ.Priority
		,	AQ.Status
		,	AQ.SendMoment
		,	AQ.Host
		,	AQ.HasAttachment
		,	MI.ListID
	from	@Mails M
			join ActiveQueue AQ on M.ID = AQ.ID
			left join Mission MI on AQ.MissionID = MI.ID
end