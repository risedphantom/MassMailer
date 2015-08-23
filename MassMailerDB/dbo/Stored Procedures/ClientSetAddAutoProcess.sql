
CREATE proc [dbo].[ClientSetAddAutoProcess]
as
begin

	--Clear table
	begin tran
		
		delete
		from	tmp.Client
		where	TozonID is null
				or SetID is null

		insert into dbo.ClientSet
		select	CWH.ClientID
			,	C.SetID
		from	tmp.Client C
				join ClientWH.matching.Client CWH on CWH.SourceClientID = C.ExternalID

		delete	tmp.Client
		from	tmp.Client C
				join ClientWH.matching.Client CWH on CWH.SourceClientID = C.ExternalID
				
	commit
    
end