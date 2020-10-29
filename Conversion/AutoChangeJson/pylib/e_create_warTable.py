#encoding : utf-8

from e_base import *

def export_json(xls, fn):
    f = create_file(fn)
    if f != None:
        reader = xls_reader.XLSReader()
        cfgs = reader.GetSheetByIndex(xls, 11, 2)
        if cfgs != None:
            f.write("{\n")
            s = "\t\"WarTable\": [\n"
            for c in cfgs:
                ri = RowIndex(len(c))
                ss = "\t\t{\n"
                ss += "\t\t\t\"warId\": \"" + conv_int(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"warName\": \"" + conv_str_bin(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"warIntro\": \"" + conv_str_bin(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"startPoint\": \"" + conv_int(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"counts\": \"" + conv_int(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"reward1\": \"" + conv_str_bin(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"reward2\": \"" + conv_str_bin(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"reward3\": \"" + conv_str_bin(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"YuanBaoR\": \"" + conv_int(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"YuQueR\": \"" + conv_int(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"TiLiR\": \"" + conv_int(c[ri.Next()]) + "\"\n"
                ss += "\t\t},\n"
                s += ss
            s = s[:-2]
            s += "\n"
            s += "\t]\n"
            s += "}"
            f.write(s)
        else:
            print('sheed %s get failed.' % 'cfg')
        f.close()
def export_bin(xls, fn):
    pass