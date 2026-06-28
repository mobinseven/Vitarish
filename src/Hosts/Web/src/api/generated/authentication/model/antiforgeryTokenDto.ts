export interface AntiforgeryTokenDto {
  requestToken?: string | null;
  headerName: string;
}
