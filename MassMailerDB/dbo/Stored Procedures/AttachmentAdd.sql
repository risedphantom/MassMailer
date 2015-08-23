	
CREATE proc dbo.AttachmentAdd
	@MessageID bigint,
	@Name varchar(8000),
	@Data varbinary(max)
as
begin
	declare @AttachmentID bigint
	
	begin tran
	
		insert Attachment(Name, Data)
		values(@Name, @Data)
		
		set @AttachmentID = scope_identity()	

		insert MailAttachment(MessageID, AttachmentID)
		values(@MessageID, @AttachmentID)

	commit
end