
CREATE function [dbo].[TruncTime](@d datetime)
	returns datetime
as
begin
	return dateadd(day,datediff(day,'',@d),'')
end