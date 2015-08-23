	
CREATE proc dbo.GetAttachments
    @MessageID bigint
as
begin
	select	AttachmentID
	from	MailAttachment
	where	MessageID = @MessageID
end