export type RootStackParamList = {
  Login: undefined;
  Home: undefined;
  Tables: undefined;
  OrderDetail: { orderId: string };
  CreateOrder: { tableId: string };
};
