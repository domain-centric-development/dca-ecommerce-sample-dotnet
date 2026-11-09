-- The outbox: one row per captured event, written in the transaction of the work that caused it, and one row per
-- consumer with its own delivery progress
CREATE TABLE event_publications (
  id UUID PRIMARY KEY,
  -- the event's type and assembly, without a version
  event_type TEXT NOT NULL,
  payload TEXT NOT NULL,
  registered_on TIMESTAMP WITH TIME ZONE NOT NULL,
  status VARCHAR(16) NOT NULL,
  attempts INT NOT NULL,
  last_error TEXT,
  completed_on TIMESTAMP WITH TIME ZONE,
  consumers_initialized BOOLEAN NOT NULL
);

CREATE INDEX idx_event_publications_status ON event_publications(status);

CREATE TABLE event_publication_deliveries (
  publication_id UUID NOT NULL REFERENCES event_publications(id) ON DELETE CASCADE,
  consumer_id VARCHAR(255) NOT NULL,
  status VARCHAR(16) NOT NULL,
  attempts INT NOT NULL,
  last_error TEXT,
  completed_on TIMESTAMP WITH TIME ZONE,
  next_attempt_on TIMESTAMP WITH TIME ZONE,
  PRIMARY KEY (publication_id, consumer_id)
);
