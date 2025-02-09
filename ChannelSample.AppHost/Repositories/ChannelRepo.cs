using ChannelSample.AppHost.Models;
using Microsoft.Data.Sqlite;

namespace ChannelSample.AppHost.Repositories;

internal class ChannelRepo(
	SqliteConnection connection,
	TimeProvider timeProvider) : IChannelRepo
{
	public Task InitialAsync(CancellationToken cancellationToken = default)
	{
		connection.Open();

		var command = connection.CreateCommand();
		command.CommandText =
		"""
			CREATE TABLE IF NOT EXISTS message
						 (
									  id INTEGER PRIMARY KEY autoincrement,
									  application text NOT NULL,
									  message text NOT NULL,
									  seq INTEGER NOT NULL,
									  message_at text NOT NULL,
									  created_at text NOT NULL
						 );
		""";
		return command.ExecuteNonQueryAsync(cancellationToken: cancellationToken);
	}

	public Task InsertAsync(string application, string message, int sequence, DateTimeOffset messageAt, CancellationToken cancellationToken = default)
	{
		connection.Open();

		var command = connection.CreateCommand();

		command.CommandText =
		@"
			INSERT INTO message (application, message , seq , message_at , created_at)
			VALUES ($application, $message , $seq , $messageAt , $createdAt)
                ";
		command.Parameters.AddWithValue("$application", application);
		command.Parameters.AddWithValue("$message", message);
		command.Parameters.AddWithValue("$seq", sequence);
		command.Parameters.AddWithValue("$messageAt", messageAt.ToString("O"));
		command.Parameters.AddWithValue("$createdAt", timeProvider.GetUtcNow().ToString("O"));

		return command.ExecuteNonQueryAsync(cancellationToken: cancellationToken);
	}

	public IAsyncEnumerable<ChannelInfo> QueryAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
