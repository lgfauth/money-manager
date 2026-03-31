import Link from "next/link";
import { CheckCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";

export default function AccountDeletedPage() {
  return (
    <Card className="border-0 bg-card/80 shadow-xl backdrop-blur-sm text-center">
      <CardHeader className="space-y-2">
        <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-income/10">
          <CheckCircle className="h-8 w-8 text-income" />
        </div>
        <CardTitle className="text-xl">Conta excluida com sucesso</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3 text-sm text-muted-foreground">
        <p>
          Sua conta e todos os dados associados foram permanentemente removidos
          do nosso sistema, conforme previsto pela LGPD.
        </p>
        <p>
          Se voce mudar de ideia, podera criar uma nova conta a qualquer
          momento.
        </p>
      </CardContent>
      <CardFooter className="justify-center">
        <Link href="/register">
          <Button>Criar nova conta</Button>
        </Link>
      </CardFooter>
    </Card>
  );
}
