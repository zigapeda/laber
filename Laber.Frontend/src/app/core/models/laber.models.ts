export interface MessageDto {
  id: number;
  channel: string;
  sender: string;
  text: string;
  receivedAt: string;
}

export interface ChannelDto {
  name: string;
  unreadCount: number;
  lastMessageAt: string | null;
}

export interface ConnectionStatusDto {
  isConnected: boolean;
  server: string;
  port: number;
  nick: string;
  lastConnectedAt: string | null;
  lastError: string | null;
}

export type ThemeMode = 'light' | 'dark' | 'system';
