
CREATE proc dbo.RobotArchivator
as
begin
	declare @Mails table(
			ID bigint primary key
		,	TemplateID bigint
		,	XMLData xml
		,	AddressFrom varchar(8000)
		,	AddressTo varchar(8000)
		,	Subject varchar(8000)
		,	Priority int
		,	Status int
		,	MissionID int
		,	ExternalOwnerID bigint
		,	SendMoment datetime
		,	AddMoment datetime
		,	Host varchar(128)
		,	AddressCC varchar(8000)
		,	HasAttachment bit)

	delete from ActiveQueue
	output deleted.* into @Mails
	where SendMoment is not null

	insert	into Archive(ID,TemplateID,XMLData,AddressFrom,AddressTo,Subject,Priority,Status,MissionID,ExternalOwnerID,SendMoment,AddMoment,Host,MailingID,AddressCC,HasAttachment)
	select	M.ID
		,	M.TemplateID 
		,	M.XMLData 
		,	M.AddressFrom 
		,	M.AddressTo 
		,	M.Subject 
		,	M.Priority
		,	M.Status 
		,	M.MissionID 
		,	M.ExternalOwnerID 
		,	M.SendMoment 
		,	M.AddMoment 
		,	M.Host 
		,	MI.MailingID
		,	M.AddressCC 
		,	HasAttachment
	from	@Mails M
			left join Mission MI on M.MissionID = MI.ID
end